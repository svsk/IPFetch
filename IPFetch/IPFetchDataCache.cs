using System;
using System.IO;
using log4net;
using Newtonsoft.Json;

namespace IPFetch
{
    public class IPFetchDataCache
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [JsonIgnore]
        public string CachePath { get; set; }

        #region Cached Properties

        [JsonProperty("currentIP")]
        public string CachedIP { get; set; }

        [JsonProperty("notificationSentSinceLastChange")]
        public bool NotificationSentSinceLastChange { get; set; }

        #endregion

        #region Load / Save

        public static IPFetchDataCache GetOrCreate(string cachePath)
        {
            IPFetchDataCache dataCache = new IPFetchDataCache();

            if (File.Exists(cachePath))
            {
                try
                {
                    dataCache = JsonConvert.DeserializeObject<IPFetchDataCache>(File.ReadAllText(cachePath));
                }
                catch (Exception ex)
                {
                    Logger.Error("Unable to load cache file.", ex);
                }
            }

            dataCache.CachePath = cachePath;
            return dataCache;
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(CachePath, JsonConvert.SerializeObject(this));
            }
            catch (Exception ex)
            {
                Logger.Error("Unable to save cache file.", ex);
            }
        }

        #endregion
    }
}