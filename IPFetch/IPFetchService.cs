using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using RestSharp;
using RestSharp.Authenticators;

namespace IPFetch
{
    public partial class IPFetchService : ServiceBase
    {
        private readonly ILog _logger;

        private bool _keepRunning = true;
        private int _checkIntervalSec;
        private string _cachedIP;
        private string _receiverEmail;
        private string _receiverName;
        private string _machineName;
        private string _cachePath;
        private string[] _dnsUpdateUrls;
        private string _ipAddressProviderUrl;
        private string _mailgunAPIKey;
        private string _mailgunDomainName;

        public IPFetchService()
        {
            _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            log4net.Config.XmlConfigurator.Configure();

            InitializeComponent();
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            try
            {
                _receiverEmail = ConfigurationManager.AppSettings["receiverEmail"];
                _receiverName = ConfigurationManager.AppSettings["receiverName"];
                _cachePath = ConfigurationManager.AppSettings["cacheFilePath"];
                _checkIntervalSec = int.Parse(ConfigurationManager.AppSettings["ipCheckIntervalSeconds"]);
                _dnsUpdateUrls = ConfigurationManager.AppSettings["dnsUpdateUrls"].Split(',');
                _ipAddressProviderUrl = ConfigurationManager.AppSettings["ipAddressProviderUrl"];
                _mailgunAPIKey = ConfigurationManager.AppSettings["mailgunAPIKey"];
                _mailgunDomainName = ConfigurationManager.AppSettings["mailgunDomainName"];
                _machineName = Environment.MachineName;
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to initialize settings", ex);
                _keepRunning = false;
            }
        }


        protected override void OnStart(string[] args)
        {
            _logger.Info("IPFetch is starting...");
            Task.Run(() => { StartCheckingIP(); }, CancellationToken.None);
        }

        protected override void OnStop()
        {
            _keepRunning = false;
            _logger.Info("IPFetch is shutting down.");
        }

        private void StartCheckingIP()
        {
            while (_keepRunning)
            {
                CheckIP();
                Thread.Sleep(_checkIntervalSec * 1000);
            }
        }

        private void CheckIP()
        {
            var currentIP = GetCurrentIP();
            _cachedIP = string.IsNullOrEmpty(_cachedIP) ? ReadIPFromFile() : _cachedIP;

            if (currentIP != _cachedIP && currentIP != null)
            {
                _logger.Info("IP has changed!");

                WriteIPToFile(currentIP);
                _cachedIP = currentIP;
                SendEmail(currentIP);
                UpdateDNS();
            }
        }

        private string GetCurrentIP()
        {
            try
            {
                var externalIP = new WebClient().DownloadString(_ipAddressProviderUrl);
                return externalIP.TrimEnd();
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to get IP address from provider", ex);
                return null;
            }
        }

        private string ReadIPFromFile()
        {
            try
            {
                return File.ReadAllText(_cachePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return string.Empty;
            }
        }

        private void WriteIPToFile(string fileContent)
        {
            _logger.Info("Writing IP to cache file");

            try
            {
                File.WriteAllText(_cachePath, fileContent);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private void SendEmail(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(_mailgunAPIKey) || string.IsNullOrWhiteSpace(_mailgunDomainName))
            {
                _logger.Warn("Mailgun configuration incomplete. Unable to send e-mail notification.");
                return;
            }

            _logger.Info("Sending notification email");

            RestClient client = new RestClient();
            client.BaseUrl = new Uri("https://api.mailgun.net/v3");
            client.Authenticator = new HttpBasicAuthenticator("api", $"{_mailgunAPIKey}");
            RestRequest request = new RestRequest();
            request.AddParameter("domain", _mailgunDomainName, ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", $"IPFetch <ipfetch-noreply@{_mailgunDomainName}>");
            request.AddParameter("to", $"{_receiverName} <{_receiverEmail}>");
            request.AddParameter("subject", $"IPFetch update for {_machineName}");
            request.AddParameter("text", $"Hi {_receiverName},\r\n\r\nThe IP for your machine ({_machineName}) has changed since we last checked and is now: {ipAddress}");
            request.Method = Method.POST;

            client.Execute(request);
        }

        private void UpdateDNS()
        {
            if (_dnsUpdateUrls != null)
            {
                _logger.Info("Updating DNS");

                foreach (var url in _dnsUpdateUrls)
                {
                    try
                    {
                        var response = new WebClient().DownloadString(url);
                        _logger.Info(response);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to update DNS ({url})", ex);
                    }
                }

                _logger.Info("Finished updating DNS");
            }
        }
    }
}
