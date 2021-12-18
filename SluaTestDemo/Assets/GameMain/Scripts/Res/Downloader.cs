using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 下载器 工具类
/// </summary>
public class Downloader : Singleton<Downloader> {

    /// <summary>
    /// 根据模块的配置，下载对应的模块
    /// </summary>
    /// <param name="moduleConfig"></param>
    /// <returns></returns>
    public async Task<bool> Download(ModuleConfig moduleConfig)
    {
        // 从服务器上下载下来的资源存放的路径
        string updatePath = GetUpdateURL(moduleConfig.moduleName);
        
        // 资源服务器上AB资源配置文件对应的URL
        string configURL = GetServerURL(moduleConfig, moduleConfig.moduleName.ToLower() + ".json");
        
        UnityWebRequest request = UnityWebRequest.Get(configURL);

        if (!Directory.Exists(updatePath))
        {
            Directory.CreateDirectory(updatePath);
        }
        request.downloadHandler = new DownloadHandlerFile($"{updatePath}/{moduleConfig.moduleName.ToLower()}_temp.json"); //临时文件
        Debug.Log("下载到本地路径：" + updatePath);
        await request.SendWebRequest();
        
        if (string.IsNullOrEmpty(request.error) == false)
        {
            return false;
        }

        List<BundleInfo> downloadList = await GetDownLoadList(moduleConfig.moduleName);

        long downLoadSize = CalculateSize(downloadList);
        if (downLoadSize == 0)
        {
            return true;
        }

        bool boxResult = await ShowMessageBox(moduleConfig, downLoadSize);
        if (boxResult == false)
        {
            Application.Quit();
            return false;
        }
        
        List<BundleInfo> remainList = await ExecuteDownload(moduleConfig, downloadList);

        if (remainList.Count > 0)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// 执行下载行为
    /// </summary>
    /// <param name="moduleConfig"></param>
    /// <param name="bundleList">包含还未下载的bundle</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<List<BundleInfo>> ExecuteDownload(ModuleConfig moduleConfig, List<BundleInfo> bundleList)
    {
        while (bundleList.Count > 0)
        {
            BundleInfo bundleInfo = bundleList[0];
            UnityWebRequest request = UnityWebRequest.Get(GetServerURL(moduleConfig, bundleInfo.bundle_name));
            string updatePath = GetUpdateURL(moduleConfig.moduleName);
            request.downloadHandler = new DownloadHandlerFile($"{updatePath}/{bundleInfo.bundle_name}");
            await request.SendWebRequest();

            // 可行性待测试
            if (request.isDone)
            {
                Debug.Log("下载资源：" + bundleInfo.bundle_name + "成功!");
                bundleList.RemoveAt(0);
            }
            else
            {
                break;;
            }
        }

        return bundleList;
    }

    /// <summary>
    /// 返回该模块所需要的bundle列表
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<List<BundleInfo>> GetDownLoadList(string moduleName)
    {
        ModuleABConfig serverConfig = await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Update, moduleName,
            moduleName.ToLower() + "_temp.json");
        if (serverConfig == null)
        {
            return null;
        }

        ModuleABConfig localConfig = await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Update, moduleName,
            moduleName.ToLower() + ".json");

        List<BundleInfo> diffList = CalculateDiff(moduleName, localConfig, serverConfig);
        return diffList;
    }
    

    /// <summary>
    /// 返回资源下载路径
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    private string GetUpdateURL(string moduleName)
    {
        return Application.persistentDataPath + "/Bundles/" + moduleName;
    }

    /// <summary>
    /// 返回资源服务器的URL
    /// </summary>
    /// <param name="moduleConfig"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public string GetServerURL(ModuleConfig moduleConfig, string fileName)
    {
#if UNITY_ANDROID
        return $"{moduleConfig.DownLoadURL}/StandaloneWindows64/{fileName}";
#elif UNITY_IOS
        return $"{moduleConfig.DownLoadURL}/StandaloneWindows64/{fileName}";
#elif UNITY_STANDALONE_WIN
        return $"{moduleConfig.DownLoadURL}/StandaloneWindows64/{fileName}";
#endif
    }
    
    /// <summary>
    /// 比对两个AB配置文件
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="localConfig"></param>
    /// <param name="serverConfig"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private List<BundleInfo> CalculateDiff(string moduleName, ModuleABConfig localConfig, ModuleABConfig serverConfig)
    {
        List<BundleInfo> bundleList = new List<BundleInfo>();
        Dictionary<string, BundleInfo> localBundleDic = new Dictionary<string, BundleInfo>();

        if (localConfig != null)
        {
            foreach (BundleInfo bundleInfo in localConfig.BundleArray.Values)
            {
                string uniqueId = $"{bundleInfo.bundle_name}|{bundleInfo.crc}";
                localBundleDic.Add(uniqueId, bundleInfo);
            }
        }
        
        // 找到有差异的Bundle文件，并加入bundleList
        foreach (BundleInfo bundleInfo in serverConfig.BundleArray.Values)
        {
            string uniqueId = $"{bundleInfo.bundle_name}|{bundleInfo.crc}";
            if (localBundleDic.ContainsKey(uniqueId) == false)
            {
                bundleList.Add(bundleInfo);
            }
            else
            {
                localBundleDic.Remove(uniqueId);
            }
        }

        string updatePath = GetUpdateURL(moduleName);
        
        // 清除本地没有用的bundle文件
        BundleInfo[] removeList = localBundleDic.Values.ToArray();
        for (int i = removeList.Length - 1; i >= 0; i--)
        {
            BundleInfo bundleInfo = removeList[i];
            string filePath = $"{updatePath}/{bundleInfo.bundle_name}";
            File.Delete(filePath);
        }
        
        // 删除旧的配置文件
        string oldFile = $"{updatePath}/{moduleName.ToLower()}.json";
        if (File.Exists(oldFile))
        {
            File.Delete(oldFile);
        }
        
        // 换为新的配置文件
        string newFile = $"{updatePath}/{moduleName.ToLower()}_temp.json";
        File.Move(newFile, oldFile);
        return bundleList;
    }

    /// <summary>
    /// 计算需要下载的资源大小
    /// </summary>
    private static long CalculateSize(List<BundleInfo> bundleInfos)
    {
        long totalSize = 0;
        foreach (BundleInfo bundleInfo in bundleInfos)
        {
            totalSize += bundleInfo.size;
        }

        return totalSize;
    }
    
    /// <summary>
    /// 弹出对话框
    /// </summary>
    private static async Task<bool> ShowMessageBox(ModuleConfig moduleConfig, long totalSize)
    {
        string downLoadSize = SizeToString(totalSize);
        string messageInfo = $"发现新版本，版本号为：{moduleConfig.moduleVersion}\n需要下载热更包，大小为:{downLoadSize}";
        
        MessageBox messageBox = new MessageBox(messageInfo,"开始下载","退出游戏");
        MessageBox.BoxResult result = await messageBox.GetReplyAsync();
        messageBox.Close();
        if (result == MessageBox.BoxResult.First)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 得到下载大小信息
    /// </summary>
    /// <param name="totalSize"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static string SizeToString(long totalSize)
    {
        Debug.Log(totalSize);
        return $"{totalSize / 1024}[K]{totalSize % 1024}[B]";
    }
}
