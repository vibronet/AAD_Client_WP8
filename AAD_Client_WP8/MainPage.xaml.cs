using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using AAD_Client_WP8.Resources;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Phone.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Phone.Storage.SharedAccess;
using Windows.Storage;
using System.IO;

namespace AAD_Client_WP8
{
    public partial class MainPage : PhoneApplicationPage
    {
        App app;
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            app = App.Current as App;
            app.Response = "";
        }

        // drives the main app UI
        protected async override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            
            if (NavigationContext.QueryString.ContainsKey("fileToken"))
            {
                // we got called because the user clicked on a .tknrq file, contianing token request parameters
                // copy the incoming file in local storage (we have no other way of accessing it)
                // open the local copy
                // parse it and assing values to the current UI
    
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                string filename = "tmp.tknrqlocal";
                try
                {
                    await SharedStorageAccessManager.CopySharedFileAsync(folder, filename,
                        NameCollisionOption.ReplaceExisting,
                        NavigationContext.QueryString["fileToken"]);

                    NavigationContext.QueryString.Clear();

                    var file = await folder.OpenStreamForReadAsync(filename);
                    using (StreamReader sr = new StreamReader(file))
                    {
                        string filetext = sr.ReadToEnd();
                        JObject jo = JsonConvert.DeserializeObject(filetext) as JObject;

                        txtAuthority.Text = (string)jo["authority"];
                        txtResource.Text = (string)jo["resource"];
                        txtClientID.Text = (string)jo["clientid"];
                        txtRedirectUri.Text = (string)jo["redirecturi"];
                        spTOkenParamsForm.Visibility = Visibility.Visible;
                        spResults.Visibility = Visibility.Collapsed;
                    }
                }
                catch
                {                    
                    MessageBox.Show("Something went wrong while processing your file");
                    spTOkenParamsForm.Visibility = Visibility.Visible;
                    spResults.Visibility = Visibility.Collapsed;
                }

                app.Response = string.Empty;
            }

            
            
            if(app.Response==string.Empty)
            {
                // if the app.Response is empty, that means that we need to show the UI for composing a request

                spTOkenParamsForm.Visibility =  Visibility.Visible;
                spResults.Visibility = Visibility.Collapsed;
            } else
            {
                // if there is stuff, we want to show it!

                spTOkenParamsForm.Visibility =  Visibility.Collapsed;
                spResults.Visibility = Visibility.Visible;

                JObject jo = JsonConvert.DeserializeObject(app.Response) as JObject;
                try
                {
                    txtAccess.Text = (string)jo["access_token"];
                    txtRefresh.Text = (string)jo["refresh_token"];
                    txtID.Text = DecodeIdToken((string)jo["id_token"]);
                }
                catch
                { 
                    // if we're here, most likely the request didn't succeed
                    // the response will contian details about the error

                    MessageBox.Show(app.Response);
                    spTOkenParamsForm.Visibility = Visibility.Visible;
                    spResults.Visibility = Visibility.Collapsed;
                }
            }            
        }

        // utility for decoding the ID token and present it in human-readable format
        private string DecodeIdToken(string start)
        {
            if (start == null)
                return "[No id_token returned]";
            
            string idcmp = start.Split('.')[1];
            if ((idcmp.Length % 4) != 0)
            {
                idcmp = idcmp.PadRight(idcmp.Length + (4 - (idcmp.Length % 4)), '=');
            }
            byte[] dec = Convert.FromBase64String(idcmp);
            JObject jo = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(dec, 0, dec.Count())) as JObject;
            return JsonConvert.SerializeObject(jo,Formatting.Indented);
        }

        // poor's man binding
        private void SyncRequestCoordinates()
        {
            app.RedirectUri = txtRedirectUri.Text;
            app.Authority = txtAuthority.Text;
            app.ClientID = txtClientID.Text;
            app.Resource = txtResource.Text;
        }

        // trigger the request flow
        private void btnSignIn_Click(object sender, RoutedEventArgs e)
        {
            SyncRequestCoordinates();
            NavigationService.Navigate(new Uri("/SignInPage.xaml", UriKind.Relative));
        }

        // show some docs
        private void About_Click(object sender, EventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri("http://www.cloudidentity.com/blog/2014/02/16/a-sample-windows-phone-8-app-getting-tokens-from-windows-azure-ad-and-adfs/");
            task.Show();
        }
       
        // put in the clipboard whatever is currently shown in the UI
        private void Copy_Click(object sender, EventArgs e)
        {
            string toBeCopied = (spTOkenParamsForm.Visibility == Visibility.Visible) ?
                String.Format(@"{{ ""authority"" : ""{0}"",""clientid"" : ""{1}"",""redirecturi"" : ""{2}"",""resource"" : ""{3}""}}", 
                                  txtAuthority.Text, txtClientID.Text, txtRedirectUri.Text, txtResource.Text) :
                String.Format(@"{{ ""access_token"" : ""{0}"",""refresh_token"" : ""{1}"",""id"" : ""{2}"" }}", txtAccess.Text, txtRefresh.Text, txtID.Text);
            Clipboard.SetText(toBeCopied);
        }       
    }
}