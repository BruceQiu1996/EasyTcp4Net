using System.IO;

namespace FileTransfer.Helpers
{
    public class IniSettings
    {
        #region 配置keys
        public const string ApplicationSectionKey = nameof(ApplicationSectionKey);
        public const string PortKey = nameof(PortKey);
        public const string AgreeTransferKey = nameof(AgreeTransferKey);
        public const string AgreeConnectKey = nameof(AgreeConnectKey);
        public const string FileSaveLocationKey = nameof(FileSaveLocationKey);
        #endregion

        #region 配置项
        public ushort Port { get; private set; }
        public bool AgreeTransfer { get; private set; }
        public bool AgreeConnect { get; private set; }
        public string FileSaveLocation { get; private set; }
        #endregion

        private readonly IniHelper _iniHelper;
        public IniSettings(IniHelper iniHelper)
        {
            _iniHelper = iniHelper;
            _iniHelper.Initialize(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini"));
        }

        public async Task InitializeAsync()
        {
            Port = ushort
                .TryParse(await _iniHelper.ReadAsync(ApplicationSectionKey, PortKey), out var tempPortKey) ? tempPortKey : default;
            AgreeTransfer = bool.TryParse(await _iniHelper.ReadAsync(ApplicationSectionKey, AgreeTransferKey), out var tempAgreeTransferKey) ? tempAgreeTransferKey : false;
            AgreeConnect = bool.TryParse(await _iniHelper.ReadAsync(ApplicationSectionKey, AgreeConnectKey), out var tempAgreeConnectKey) ? tempAgreeConnectKey : false;
            FileSaveLocation = await _iniHelper.ReadAsync(ApplicationSectionKey, FileSaveLocationKey);
        }

        public async Task WritePortAsync(ushort port)
        {
            await _iniHelper.WriteAsync(ApplicationSectionKey, PortKey, port.ToString());
            Port = port;
        }

        public async Task WriteAgreeTransferAsync(bool value)
        {
            await _iniHelper.WriteAsync(ApplicationSectionKey, AgreeTransferKey, value.ToString());
            AgreeTransfer = value;
        }

        public async Task WriteAgreeConnectAsync(bool value)
        {
            await _iniHelper.WriteAsync(ApplicationSectionKey, AgreeConnectKey, value.ToString());
            AgreeConnect = value;
        }

        public async Task WriteFileSaveLocationAsync(string location)
        {
            await _iniHelper.WriteAsync(ApplicationSectionKey, FileSaveLocationKey, location);
            FileSaveLocation = location;
        }
    }
}
