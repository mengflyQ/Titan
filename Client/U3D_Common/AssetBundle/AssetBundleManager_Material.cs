﻿/*******************************************************************
** 文件名:	AssetBundleManager_Material.cs
** 版  权:	(C) 深圳冰川网络技术有限公司 2014 - Speed
** 创建人:	谭强均
** 日  期:	2015/10/26
** 版  本:	1.0
** 描  述:	ab资源管理器
** 应  用:  	ab资源管理器  材质

**************************** 修改记录 ******************************
** 修改人: 
** 日  期: 
** 描  述: 
********************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public sealed partial class AssetBundleManager : MonoBehaviour
{

    /// <summary>
    ///  正在使用资源GUID，资源结点   与m_MaterialCacheResTable是互斥的存在
    /// </summary>
    private static Dictionary<int, ResNode> m_MaterialResTable = new Dictionary<int, ResNode>();

    /// <summary>
    /// 缓存的资源结点，GUID-资源结点 与m_MaterialTable是互斥的存在
    /// </summary>
    private static Dictionary<int, ResNode> m_MaterialCacheResTable = new Dictionary<int, ResNode>();


    private static Type MaterialType = typeof(Material);


    /// <summary>
    /// 获取资源，资源内部使用引用计数来管理资源，切勿乱调用,一个get，一个delete
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    private static ResNode GetMaterialResource(ResConfigData config)
    {
        if (!isVaild)
        {
            return null;
        }

        if (string.IsNullOrEmpty(config.AssetBundleName))
        {
            return null;
        }
        ResNode res = null;

        //先看看是不是通用资源
         GetCommonRes(ref res,config);
        if (null != res)
        {
            return res;
        }

        if (IsUnionDependenicesAssetBundle(config.AssetBundleName))
        {
            if (!config.fromReference)
            {
                AssetBudleLog(StringHelper.BuildString("资源错误--uniondepndenices不能单独加载!:", config.AssetName));
                return res;
            }
        }

        //先看看缓存里是不是有这个资源
        res = GetCacheMaterialResource(config);

        if (null != res)
        {
            m_MaterialResTable.Add(res.instanceID, res);
            //判断一下这个资源是否是异步加载的
            if (res.isAsync)
            {
                if (!res.isLoadFinish)
                {
                    BuildResNodeWhenInAsyncLoading<Material>(ref res, config);
                    //Debug.LogWarning(StringHelper.BuildString(config.AssetName, "正在异步加载中，请调用ResNode.isLoadFinish查看是否完成"));
                }
            }
            res.AddRef();
            return res;
        }

        int InstanceID = GUIDToInstanceID(config.AssetGUID);
        //所需的资源已经存在
        if (m_MaterialResTable.TryGetValue(InstanceID,out res))
        {
            //判断一下这个资源是否是异步加载的
            if (res.isAsync)
            {
                if (!res.isLoadFinish)
                {
                    BuildResNodeWhenInAsyncLoading<Material>(ref res, config);
                    //Debug.LogWarning(StringHelper.BuildString(config.AssetName, "正在异步加载中，请调用ResNode.isLoadFinish查看是否完成"));
                }
            }

            //同时增加这个资源的引用计数
            res.AddRef();
        }
        else
        {
            res = BuildMaterialResNode(config);
            if (res != null)
            {
                m_MaterialResTable.Add(res.instanceID, res);
            }
        }
        if (null != res)
        {
            res.assetType = AssetType.Asset_Material;
        }
        return res;
    }

    /// <summary>
    /// 获取鼠标资源，鼠标资源只能放在通用目录下
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    private static ResNode GetMaterialResource(string path)
    {
        ResNode result = null;
        string assetName = System.IO.Path.GetFileName(path);
        string assetBundleName = System.IO.Path.GetDirectoryName(path).ToLower();

        if (assetBundleName.Equals(string.Empty))
        {
            assetBundleName = "default";
        }

        assetBundleName = "material/" + assetBundleName + ".modelmat";

        if (!CheckAssetPatch(assetBundleName, assetName))
        {
            Debug.LogWarning("找不到资源:" + path + "资源必须放在通用资源目录下[assets/commonartist]");
            return null;
        }
        else
        {

            GetCommonRes(ref result, assetBundleName, assetName, typeof(Material));
            return result;
        }
    }

    /// <summary>
    /// 删除资源
    /// </summary>
    /// <param name="res"></param>
    private static void DeleteMaterialResource(ref ResNode res, bool Immediate)
    {
        if (null == res || !isVaild ||
            res.GetRefCount() <= 0)
        {
            return;
        }

        //先看看是不是通用资源
        if (IsCommonAssets(res))
        {
            res.Release();
            return;
        }

        //只要有一个需要理解卸载，那么就得立刻卸载
        if (!res.UnLoadImmediate)
        {
            res.UnLoadImmediate = Immediate;
        }
        int guid = res.instanceID;
        //有这个资源
        if (m_MaterialResTable.ContainsKey(guid))
        {
            int refCount = res.Release();

            //这个资源已经没有人引用了
            if (refCount <= 0)
            {
                m_MaterialResTable[guid] = null;
                m_MaterialResTable.Remove(guid);

                if (!res.UnLoadImmediate)
                {
                    CacheMaterialResource(res);
                }
                else
                {
                    if (m_MaterialCacheResTable.ContainsKey(guid))
                    {
                        m_MaterialCacheResTable.Remove(guid);
                    }
                    UnLoadResource(ref res);
                }
             
            }
        }
        else
        {
            Debug.LogError("要卸载的资源不是Material,忽略此资源，有可能会存在资源泄露!:" + res.AssetBundleName + "," + res.AssetName);
        }
        res = null;
    }

    private static ResNode BuildMaterialResNode(ResConfigData config)
    {
        ResNode res = null;

        if(!isOnRunTimeMode)
        {
            BuildResNodeOnEditor<Material>(ref res, config);
        }
        else
        {
            BuildResNodeOnRunTime<Material>(ref res, config);
        }
        return res;
    }

    /// <summary>
    /// 获取资源，资源内部使用引用计数来管理资源，切勿乱调用,一个get，一个delete
    /// 返回的值仅仅是一个标示，没有资源~
    /// </summary>
    /// <param name="config">资源配置</param>
    /// <param name="callBack">回调</param>
    /// <param name="userDataObj">用户数据，这个会出现在资源回调里,不需要可以填null</param>
    /// <param name="priority">加载优先级</param>
    /// <returns></returns>
    private static ResNode GetMaterialResourceAsync(ResConfigData config, System.Action<ResConfigData, ResNode, System.Object> callBack,
                                                   System.Object userDataObj = null, AssetLoadPriority priority = AssetLoadPriority.Priority_Normal)
    {
        if (!isVaild)
        {

            return null;
        }

        if (string.IsNullOrEmpty(config.AssetBundleName))
        {
            return null;
        }
        ResNode res = null;

        //先看看是不是通用资源
        GetCommonRes(ref res,config);
        if (null != res)
        {
            PushToResDelayCallBackList(res, config, callBack, userDataObj);
            return res;
        }

        //编辑器下无异步加载
        if(!isOnRunTimeMode)
        {
            res = GetMaterialResource(config);
            PushToResDelayCallBackList(res, config, callBack, userDataObj);
            return res;
        }

        if (IsUnionDependenicesAssetBundle(config.AssetBundleName))
        {
            if (!config.fromReference)
            {
                AssetBudleLog(StringHelper.BuildString("资源错误--uniondepndenices不能单独加载!:", config.AssetName));
                return res;
            }
        }

        config.priority = priority;
        res = BuildMaterialResNodeAsync(config, callBack, userDataObj);
        if (res != null)
        {
            res.assetType = AssetType.Asset_Material;
        }
        return res;
    }

    /// <summary>
    /// 删除异步加载的资源，与GetMaterialResourceAsync对应
    /// </summary>
    /// <param name="res"></param>
    /// <param name="callBack"></param>
    private static void DeleteMaterialResourceAsync(ref ResNode res, System.Action<ResConfigData, ResNode, System.Object> callBack, bool Immediate)
    {
        if (null == res || !isVaild ||
            res.GetRefCount() <= 0)
        {
            return;
        }

        
        //先看看是不是通用资源
        if (IsCommonAssets(res))
        {
            res.Release();
            RemoveFromDelayCallBackList(res, callBack);
            return;
        }

        //只要有一个需要理解卸载，那么就得立刻卸载
        if (!res.UnLoadImmediate)
        {
            res.UnLoadImmediate = Immediate;
        }

        //编辑器下没有异步
        if(!isOnRunTimeMode)
        {
            RemoveFromDelayCallBackList(res, callBack);
            DeleteMaterialResource(ref res, Immediate);
            return;
        }
        int guid = res.instanceID;
        //有这个资源
        if (m_MaterialResTable.ContainsKey(guid))
        {
            int refCount = res.Release();

            //这个资源已经没有人引用了
            if (refCount <= 0)
            {
                RemoveAllResCallBack(m_ResCallBackTable, res);
                RemoveAllDelayCallBackList(res);

                m_MaterialResTable[guid] = null;
                m_MaterialResTable.Remove(guid);
                if (!res.UnLoadImmediate)
                {
                    CacheMaterialResource(res);
                }
                else
                {
                    if (m_MaterialCacheResTable.ContainsKey(guid))
                    {
                        m_MaterialCacheResTable.Remove(guid);
                    }
                    UnLoadResource(ref res);
                }
            }
            else
            {
                RemoveResCallBack(m_ResCallBackTable, res, callBack);
                RemoveFromDelayCallBackList(res, callBack);
            }
        }
        else
        {
            Debug.LogError("要卸载的资源不是Material,忽略此资源，有可能会存在资源泄露!:" + res.AssetBundleName + "," + res.AssetName);
        }
        res = null;

    }



    /// <summary>
    /// 异步构建ResNode，会立即返回一个ResNode，这个ResNode只是一个标示，里面没有资源!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="config"></param>
    /// <param name="callBack"></param>
    /// <param name="userDataObj"></param>
    /// <returns></returns>
    private static ResNode BuildMaterialResNodeAsync(ResConfigData config, System.Action<ResConfigData, ResNode, System.Object> callBack,
                                                    System.Object userDataObj)
    {
        ResNode res = null;

        int assetGUID = GUIDToInstanceID(config.AssetGUID);

        //如果这个资源正在要卸载的列表里，移除掉,免得资源加载完就被卸载了
        res = GetResNodeFromNeedToUnLoadAsset(assetGUID);
        //如果缓存里有这个资源结点
        if (res != null)
        {
            res.AddRef();
            m_MaterialResTable.Add(assetGUID, res);


            GenerateResCallBack(res, config, callBack, userDataObj);
            res.config = config;
            return res;
        }

        res = GetCacheMaterialResource(config);

        //如果缓存里有这个资源结点
        if (res != null)
        {
            res.AddRef();
            m_MaterialResTable.Add(assetGUID, res);

            //看看资源是不是已经加载完成了。
            if (!res.isLoadFinish)
            {
                GenerateResCallBack(res, config, callBack, userDataObj);
            }
            else
            {
                PushToResDelayCallBackList(res, config, callBack, userDataObj);
            }
            res.config = config;
            return res;
        }

        //没有在缓存里，就看看是不是已经在使用了
        if (m_MaterialResTable.ContainsKey(assetGUID))
        {
            res = m_MaterialResTable[assetGUID];
            res.AddRef();

            //看看资源是不是已经加载完成了。
            if (!res.isLoadFinish)
            {
                GenerateResCallBack(res, config, callBack, userDataObj);
            }
            else
            {
                PushToResDelayCallBackList(res, config, callBack, userDataObj);
            }
            res.config = config;
            return res;
        }
        
        /*没有缓存，没有在使用，说明这是一个全新的请求，直接走下面，创建一个新的请求*/

        res = GetMaterialPackgeAsync(config, MaterialPackageCallBack, callBack, userDataObj);
        //res = BuildResNodeOnRunTimeAsync<Texture>(config, callBack, userDataObj);
        //if (res != null)
        //{
        //    m_TextureResTable.Add(config.AssetGUID, res);
        //}
        return res;
    }

    private static ResNode GetMaterialPackgeAsync(ResConfigData config, System.Action<ResourcePackageCallBackObj> resourcePackageCallBack,
                                           System.Action<ResConfigData, ResNode, System.Object> UserCallBack, System.Object userDataObj)
    {
        ResNode res = null;
        BuildPacakgeAsync(ref res, m_MaterialResTable, config, resourcePackageCallBack, UserCallBack, userDataObj);
        return res;
    }

    private static float GetMaterialUpLoadClipTime(ResNode res)
    {
        return 1.0f / MaxUpLoad_Assets_AsyncRequestClip;
    }

    private static bool UpLoadMaterialAsyncRequest(ResourcePackageCallBackObj req)
    {
        return UpLoadAsyncRequest<Material>(req,true);
    }

    private static void MaterialPackageCallBack(ResourcePackageCallBackObj req)
    {
        EnQueueWaitForUpLoadAssetsAsyncRequestTable(MaterialType, req);
    }

    /// <summary>
    /// 缓存，避免频繁的加载
    /// </summary>
    /// <param name="res"></param>
    private static void CacheMaterialResource(ResNode res)
    {
        res.cacheTime = 0.0f;

        //缓存起来
        m_MaterialCacheResTable.Add(res.instanceID, res);
    }


    private static ResNode GetCacheMaterialResource(ResConfigData cof)
    {
        ResNode res = null;
        int InstanceID = GUIDToInstanceID(cof.AssetGUID);
        if (m_MaterialCacheResTable.TryGetValue(InstanceID, out res))
        {
            res.cacheTime = 0.0f;
            m_MaterialCacheResTable.Remove(InstanceID);
        }

        return res;
    }

    private static void ClearMaterialRes()
    {

        ClearResTable(ref m_MaterialResTable);
        ClearResTable(ref m_MaterialCacheResTable);

    }

    /// <summary>
    /// 更新纹理缓存
    /// </summary>
    /// <param name="LoadCompleteList"></param>
    private static void UpdateMaterialCache(List<int> LoadCompleteList, float dealtTime)
    {
        if (m_MaterialCacheResTable.Count > 0)
        {
            LoadCompleteList.Clear();
            foreach (KeyValuePair<int, ResNode> r in m_MaterialCacheResTable)
            {
                ResNode res = r.Value;
                res.cacheTime += dealtTime;

                //到了缓存时间，卸载资源
                if (res.cacheTime > AssetCacheTime)
                {
                    LoadCompleteList.Add(r.Key);
                    UnLoadResource(ref res);

                }
            }

            //移除已经加载完成的
            foreach (int s in LoadCompleteList)
            {
                m_MaterialCacheResTable[s] = null;
                m_MaterialCacheResTable.Remove(s);
            }
        }
    }




}
