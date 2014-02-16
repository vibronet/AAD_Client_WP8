using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Windows.Phone.Storage.SharedAccess;

namespace AAD_Client_WP8
{
    class UriMapper : UriMapperBase
    {
        private string tempUri;

        // simple mapper straight from http://msdn.microsoft.com/en-us/library/windowsphone/develop/jj206987(v=vs.105).aspx
        // used to direct file based activations to MainPage.xaml
        public override Uri MapUri(Uri uri)
        {
            tempUri = uri.ToString();
            if (tempUri.Contains("/FileTypeAssociation"))
            {                
                int fileIDIndex = tempUri.IndexOf("fileToken=") + 10;
                string fileID = tempUri.Substring(fileIDIndex);
                string incomingFileName =
                    SharedStorageAccessManager.GetSharedFileName(fileID);
                string incomingFileType = Path.GetExtension(incomingFileName);
                switch (incomingFileType)
                {
                    case ".tknrq":
                        return new Uri("/MainPage.xaml?fileToken=" + fileID, UriKind.Relative);
                    default:
                        return new Uri("/MainPage.xaml", UriKind.Relative);
                }
            }
            
            return uri;
        }
    }

}
