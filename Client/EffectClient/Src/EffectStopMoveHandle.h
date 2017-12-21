﻿/*******************************************************************
** 文件名:	EffectStopMoveHandle.h
** 版  权:	(C) 深圳冰川网络技术有限公司 2008 - All Rights Reserved
** 创建人:	彭政林
** 日  期:	5/13/2015
** 版  本:	1.0
** 描  述:	效果-停止移动操作
**************************** 修改记录 ******************************
** 修改人: 
** 日  期: 
** 描  述: 
********************************************************************/

#pragma once

#include "IEffect.h"
#include "EffectDef.h"
#include "IEntity.h"
using namespace EFFECT_CLIENT;

class CEffectStopMoveHandle : public IEffect
{
public:
	typedef  EffectClient_StopMoveHandle    SCHEME_DATA;

	CEffectStopMoveHandle( SCHEME_DATA & data ) : m_data(data){}

	// 效果启用
	virtual bool			Start( EFFECT_CONTEXT * context )
	{
		if ( context==0 || context->pEntity==0 )
			return false;

		// 告诉显示层停止移动操作
		context->pEntity->sendCommandToEntityView(ENTITY_TOVIEW_STOP_MOVE_HANDLE, m_data.nTime, 0, 0, 0);

		return true;
	}

	// 效果停止
	virtual void			Stop()
	{
	}

	// 克隆一个新效果
	virtual IEffect *       Clone() { return new CEffectStopMoveHandle(m_data); }

	// 释放
	virtual void			Release()
	{
		Stop();
		delete this;
	}

	// 取得效果ID
	virtual int				GetEffectID() { return m_data.nID; }

private:
	SCHEME_DATA             m_data;
};
