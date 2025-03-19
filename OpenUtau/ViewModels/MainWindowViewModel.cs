using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData.Binding;
using OpenUtau.App.Views;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class PartsContextMenuArgs {
        public UPart? Part { get; set; }
        public bool IsVoicePart => Part is UVoicePart;
        public bool IsWavePart => Part is UWavePart;
        public ReactiveCommand<UPart, Unit>? PartDeleteCommand { get; set; }
        public ReactiveCommand<UPart, Unit>? PartRenameCommand { get; set; }
        public ReactiveCommand<UPart, Unit>? PartGotoFileCommand { get; set; }
        public ReactiveCommand<UPart, Unit>? PartReplaceAudioCommand { get; set; }
        public ReactiveCommand<UPart, Unit>? PartTranscribeCommand { get; set; }
    }

    public class MainWindowViewModel : ViewModelBase, ICmdSubscriber {
        public bool ExtendToFrame => OS.IsMacOS();
        public string Title => !ProjectSaved
            ? $"{AppVersion}"
            : $"{(DocManager.Inst.ChangesSaved ? "" : "*")}{AppVersion} [{DocManager.Inst.Project.FilePath}]";
        [Reactive] public PlaybackViewModel PlaybackViewModel { get; set; }
        [Reactive] public TracksViewModel TracksViewModel { get; set; }
        [Reactive] public ReactiveCommand<string, Unit>? OpenRecentCommand { get; private set; }
        [Reactive] public ReactiveCommand<string, Unit>? OpenTemplateCommand { get; private set; }
        public ObservableCollectionExtended<MenuItemViewModel> OpenRecent => openRecent;
        public ObservableCollectionExtended<MenuItemViewModel> OpenTemplates => openTemplates;
        public ObservableCollectionExtended<MenuItemViewModel> TimelineContextMenuItems { get; }
            = new ObservableCollectionExtended<MenuItemViewModel>();

        [Reactive] public string ClearCacheHeader { get; set; }
        public bool ProjectSaved => !string.IsNullOrEmpty(DocManager.Inst.Project.FilePath) && DocManager.Inst.Project.Saved;
        public string AppVersion => $"OpenUtau v{System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version}";
        [Reactive] public double Progress { get; set; }
        [Reactive] public string ProgressText { get; set; }
        public ReactiveCommand<UPart, Unit> PartDeleteCommand { get; set; }
        public ReactiveCommand<int, Unit>? AddTempoChangeCmd { get; set; }
        public ReactiveCommand<int, Unit>? DelTempoChangeCmd { get; set; }
        public ReactiveCommand<int, Unit>? AddTimeSigChangeCmd { get; set; }
        public ReactiveCommand<int, Unit>? DelTimeSigChangeCmd { get; set; }
        public ReactiveCommand<Unit, Unit> OnMenuRvsynthCommand { get; }

        private ObservableCollectionExtended<MenuItemViewModel> openRecent = new();
        private ObservableCollectionExtended<MenuItemViewModel> openTemplates = new();

        public MainWindowViewModel() {
            PlaybackViewModel = new PlaybackViewModel();
            TracksViewModel = new TracksViewModel();
            ClearCacheHeader = string.Empty;
            ProgressText = string.Empty;

            OnMenuRvsynthCommand = ReactiveCommand.Create(OnMenuRvsynth);

            OpenRecentCommand = ReactiveCommand.Create<string>(file => {
                try {
                    OpenProject(new[] { file });
                } catch (Exception e) {
                    var customEx = new MessageCustomizableException("Failed to open recent", "<translate:errors.failed.openfile>: recent project", e);
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(customEx));
                }
            });

            OpenTemplateCommand = ReactiveCommand.Create<string>(file => {
                try {
                    OpenProject(new[] { file });
                    DocManager.Inst.Project.Saved = false;
                    DocManager.Inst.Project.FilePath = string.Empty;
                } catch (Exception e) {
                    var customEx = new MessageCustomizableException("Failed to open template", "<translate:errors.failed.openfile>: project template", e);
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(customEx));
                }
            });

            PartDeleteCommand = ReactiveCommand.Create<UPart>(part => {
                TracksViewModel.DeleteSelectedParts();
            });

            DocManager.Inst.AddSubscriber(this);
        }

        public void OnMenuRvsynth() {
            // TODO: Implement Rvsynth processing window logic here.
        }

        public void Undo() => DocManager.Inst.Undo();
        public void Redo() => DocManager.Inst.Redo();
        public void SaveProject(string file = "") {
            if (file == null) return;
            DocManager.Inst.ExecuteCmd(new SaveProjectNotification(file));
            this.RaisePropertyChanged(nameof(Title));
        }

        public void ImportAudio(string file) {
            if (file == null) return;
            var project = DocManager.Inst.Project;
            UWavePart part = new() { FilePath = file };
            part.Load(project);
            if (part == null) return;
            int trackNo = project.tracks.Count;
            part.trackNo = trackNo;
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new AddTrackCommand(project, new UTrack(project) { TrackNo = trackNo }));
            DocManager.Inst.ExecuteCmd(new AddPartCommand(project, part));
            DocManager.Inst.EndUndoGroup();
        }

        public void RefreshOpenRecent() {
            openRecent.Clear();
            openRecent.AddRange(Core.Util.Preferences.Default.RecentFiles.Select(file => new MenuItemViewModel() {
                Header = file,
                Command = OpenRecentCommand,
                CommandParameter = file,
            }));
        }

        public void RefreshTemplates() {
            Directory.CreateDirectory(PathManager.Inst.TemplatesPath);
            var templates = Directory.GetFiles(PathManager.Inst.TemplatesPath, "*.ustx");
            openTemplates.Clear();
            openTemplates.AddRange(templates.Select(file => new MenuItemViewModel() {
                Header = Path.GetRelativePath(PathManager.Inst.TemplatesPath, file),
                Command = OpenTemplateCommand,
                CommandParameter = file,
            }));
        }

        public void RefreshCacheSize() {
            string header = ThemeManager.GetString("menu.tools.clearcache") ?? "";
            ClearCacheHeader = header;
            Task.Run(async () => {
                var cacheSize = PathManager.Inst.GetCacheSize();
                await Dispatcher.UIThread.InvokeAsync(() => {
                    ClearCacheHeader = $"{header} ({cacheSize})";
                });
            });
        }

        #region ICmdSubscriber

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is ProgressBarNotification progressBarNotification) {
                Dispatcher.UIThread.InvokeAsync(() => {
                    Progress = progressBarNotification.Progress;
                    ProgressText = progressBarNotification.Info;
                });
            } else if (cmd is LoadProjectNotification loadProject) {
                Core.Util.Preferences.AddRecentFileIfEnabled(loadProject.project.FilePath);
            } else if (cmd is SaveProjectNotification saveProject) {
                Core.Util.Preferences.AddRecentFileIfEnabled(saveProject.Path);
            }
            this.RaisePropertyChanged(nameof(Title));
        }

        #endregion
    }
}
