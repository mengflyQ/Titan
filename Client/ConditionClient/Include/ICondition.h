﻿/*******************************************************************
** 文件名:	ICondition.h
** 版  权:	(C) 深圳冰川网络技术有限公司
** 创建人:	penglu
** 日  期:	2014/12/23
** 版  本:	1.0
** 描  述:	条件工厂相关接口
** 应  用:  	
**************************** 修改记录 ******************************
** 修改人: 
** 日  期: 
** 描  述:	
********************************************************************/

#pragma once

#include "IEntity.h"

// 通用条件现场
struct CONDITION_CONTEXT
{
    IEntity		*pEntity;
    UID			uidOperator;		// 操作者(比如接到其他玩家使用技能消息，需不需要读条，其他玩家技能结束，自己操作不操作等)
    UID         uidTarget;			// 目标
    Vector3		ptTargetTile;		// 使用技能时鼠标点击的位置
    int			nID;				// 技能ID
    DWORD       dwStartTick;        // 开始时间
    DWORD       dwUpdateFrame;      // 相对开始时间，当前是第几帧(每帧时间由UPDATE_FRAME_TIMER定义)
    int         nEventID;           // 当前的技能事件
	DWORD		dwAttackSerialID;	// 攻击对象序号

    CONDITION_CONTEXT()
    {
        memset(this, 0, sizeof(*this));
    }
};

namespace rkt
{
	/* 条件接口
	*/
	struct ICondition
	{
		// 构造条件
		virtual bool            Build( void * scheme,int len ) = 0;

		// 检查条件是否满足
		virtual bool			Test( CONDITION_CONTEXT * context ) = 0;

		// 释放
		virtual void			Release() = 0;
	};

	// 条件工厂接口
	struct IConditionFactory
	{
		// 载入条件工厂
		virtual bool           Load(const char * szFileName) = 0;

		/**
		@name				: 检测内部条件(比如技能系统注册的内置条件)
		@param	strType  	: 条件类型名
		@param  pData       : 条件数据
		@param  nLen        : 条件数据长度
		@return
		*/
		virtual bool            TestInnerCondition(const char * strType,void * pData,int nLen,CONDITION_CONTEXT * context ) = 0;

		/**
		@name				: 创建条件对象
		@param				: 条件ID
		@param				: 条件所依附的实体
		@return
		*/
		virtual ICondition*		BuildCondition(int nConditionID) = 0;

		// 释放
		virtual void			release() = 0;
	};

    ///////////////////////////////////////////////////////////////////////////////////////////////////////
    #include "LoadLib.h"
    #if defined(_LIB) || defined(CONDITIONCLIENT_STATIC_LIB)		/// 静态库版本
    #	pragma comment(lib, MAKE_LIB_NAME(ConditionClient))
        extern "C" IConditionFactory * CreateConditionFactory();
    #	define	CreateConditionFactoryProc	CreateConditionFactory
    #else													/// 动态库版本
        typedef IConditionFactory * (RKT_CDECL * procCreateConditionFactory)();
    #	define	CreateConditionFactoryProc	DllApi<procCreateConditionFactory>::load(MAKE_DLL_NAME(ConditionClient), "CreateConditionFactory")
    #endif
}