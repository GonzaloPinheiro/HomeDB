
namespace HomeDB.Domain.Common
{
    public class AuditLogActions
    {
        public const string Login = "LOGIN";
        public const string Register = "REGISTER";
        public const string Logout = "LOGOUT";
        public const string ChangePassword = "CHANGE_PASSWORD";
        public const string UploadFile = "UPLOAD_FILE";
        public const string DownloadFile = "DOWNLOAD_FILE";
        public const string DeleteFile = "DELETE_FILE";
        public const string CreateFolder = "CREATE_FOLDER";
        public const string DeleteFolder = "DELETE_FOLDER";
    }
}