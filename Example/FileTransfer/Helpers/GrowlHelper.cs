using HandyControl.Controls;
using HandyControl.Data;

namespace FileTransfer.Helpers
{
    public class GrowlHelper
    {
        public void Info(string message)
        {
            Growl.Info(new GrowlInfo()
            {
                Message = message,
                ShowDateTime = false,
                ShowCloseButton = true,
                StaysOpen = false,
                WaitTime = 3
            });
        }

        public void Success(string message)
        {
            Growl.Success(new GrowlInfo()
            {
                Message = message,
                ShowDateTime = false,
                ShowCloseButton = true,
                StaysOpen = false,
                WaitTime = 3
            });
        }

        public void Warning(string message)
        {
            Growl.Warning(new GrowlInfo()
            {
                Message = message,
                ShowDateTime = false,
                ShowCloseButton = true,
                StaysOpen = false,
                WaitTime = 3
            });
        }

        public void InfoGlobal(string message)
        {
            Growl.InfoGlobal(new GrowlInfo()
            {
                Message = message,
                ShowDateTime = false,
                ShowCloseButton = true,
                StaysOpen = false,
                WaitTime = 3
            });
        }

        public void SuccessGlobal(string message)
        {
            Growl.SuccessGlobal(new GrowlInfo()
            {
                Message = message,
                ShowDateTime = false,
                ShowCloseButton = true,
                StaysOpen = false,
                WaitTime = 3
            });
        }

        public void WarningGlobal(string message)
        {
            Growl.WarningGlobal(new GrowlInfo()
            {
                Message = message,
                ShowDateTime = false,
                ShowCloseButton = true,
                StaysOpen = false,
                WaitTime = 3
            });
        }
    }
}
