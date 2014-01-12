namespace MetroExplorer.Core.Utils
{
    using System;

    public class EventLogger
    {
        private const String UmengAppKey = "5151b11f56240bba2a002fbd";

        public static readonly String LabelHomePage = "home_page";//
        public static readonly String LabelExplorerPage = "explorer_page";//

        public static readonly String AddFolderClick = "add_folder_click";//
        public static readonly String AddFolderDone = "add_folder_done";//
        public static readonly String AddFolderCancel = "add_folder_cancel";//

        public static readonly String FolderOpened = "folder_opened";//
        public static readonly String FileOpened = "file_opened";//

        public static readonly String PhotoViewed = "photo_viewed";//

        public static readonly String SupportUs = "support_us";//
        public static readonly String LanguagesSettings = "languages_settings";//

        public static readonly String ParamLanguagesEn = "en";//
        public static readonly String ParamLanguagesFr = "fr";//
        public static readonly String ParamLanguagesZh = "zh";//


        public static void OnLaunch()
        {
            #if DEBUG
            UmengSDK.UmengAnalytics.setDebug(true);
            #endif
            UmengSDK.UmengAnalytics.setSessionContinueInterval(TimeSpan.FromSeconds(30));
            UmengSDK.UmengAnalytics.onLaunching(UmengAppKey);
        }

        public static void OnActionEvent(String eventId)
        {
            UmengSDK.UmengAnalytics.onEvent(eventId);
        }

        public static void OnActionEvent(String eventId, String label)
        {
            UmengSDK.UmengAnalytics.onEvent(eventId, label);
        }
    }
}
