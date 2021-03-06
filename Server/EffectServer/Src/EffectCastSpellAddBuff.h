﻿/*******************************************************************
** 文件名:	EffectCastSpellAddBuff.h
** 版  权:	(C) 深圳冰川网络技术有限公司 2008 - All Rights Reserved
** 创建人:	彭政林
** 日  期:	8/18/2016
** 版  本:	1.0
** 描  述:	效果-使用技能增加BUFF
需求描述：http://172.16.0.120/redmine/issues/2158
实现描述：使用特定技能增加BUFF
********************************************************************/

#pragma once

#include "IEffect.h"
#include "EffectDef.h"
#include "IEntity.h"
using namespace EFFECT_SERVER;
#include "IEventEngine.h"
#include "IEntityEvent.h"

class CEffectCastSpellAddBuff : public IEffectEx,public IEventExecuteSink
{
public:
	typedef  EffectServer_CastSpellAddBuff    SCHEME_DATA;

	CEffectCastSpellAddBuff( SCHEME_DATA & data ) : m_data(data),m_pEntity(0)
	{
        vector<string> tempVector;
        tempVector.clear();

        m_SpellIDVector.clear();
        StringHelper::split(tempVector, m_data.strSpellID, ';', " ");
        for (UINT nCount=0; nCount<tempVector.size(); ++nCount)
        {
            m_SpellIDVector.push_back(StringHelper::toInt(tempVector[nCount]));
        }
	}

    // 获取效果ID
    virtual int             GetID()
    {
        return m_data.nID;
    }

	// 效果启用
	virtual bool			Start( EFFECT_CONTEXT * context,CONDITION_CONTEXT *pConditionContext )
	{
		if ( context==0 || context->pEntity==0 )
			return false;

		m_pEntity = context->pEntity;

		g_EHelper.Subscibe(m_pEntity, this, EVENT_ENTITY_CAST_SPELL, "CEffectCastSpellAddBuff");

		return true;
	}

	// 效果停止
	virtual void			Stop()
	{
		if (m_pEntity != 0)
		{
			g_EHelper.UnSubscibe(m_pEntity, this, EVENT_ENTITY_CAST_SPELL);

			m_pEntity = 0;
		}
	}

	// 克隆一个新效果
	virtual IEffect *       Clone()
	{
		return new CEffectCastSpellAddBuff(m_data);
	}

	// 释放
	virtual void			Release()
	{
		Stop();
		delete this;
	}

	/////////////////////////////////////////IEventExecuteSink/////////////////////////////////////////
	/** 
	@param   wEventID ：事件ID
	@param   bSrcType ：发送源类型
	@param   dwSrcID : 发送源标识（实体为UID中"序列号"部份，非实体就为0）
	@param   pszContext : 上下文
	@param   nLen : 上下文长度
	@return  
	@note     
	@warning 
	@retval buffer 
	*/
	virtual void OnExecute(WORD wEventID, BYTE bSrcType, DWORD dwSrcID, LPCSTR pszContext, int nLen)
	{
		switch (wEventID)
		{
		case EVENT_ENTITY_CAST_SPELL:
			{
				if (m_pEntity == NULL)
				{
					break;
				}

				if (pszContext == NULL || nLen != sizeof(event_cast_spell))
				{
					break;
				}
				event_cast_spell *pCast = (event_cast_spell *)pszContext;

                int bFind = 0;
                int nCount = (int)m_SpellIDVector.size();
                for (int i=0; i<nCount; ++i)
                {
                    if (m_SpellIDVector[i] == pCast->nID)
                    {
                        bFind = 1;
                        break;
                    }
                }
                if (bFind != m_data.bMatching)
                {
                    break;
                }

                UID uidSelf = m_pEntity->getUID();
                switch (m_data.nType)
                {
                case BUFF_ADD:
                    {
                        SBuffContext BuffContext;
                        BuffContext.nID = pCast->nID;
                        AddBuff(uidSelf, m_data.nBuffID, m_data.nBuffLevel, uidSelf, 0, &BuffContext, sizeof(BuffContext));
                    }
                    break;
                case BUFF_REMOVE:
                    {
                        RemoveBuff(uidSelf, m_data.nBuffID, uidSelf, uidSelf);
                    }
                    break;
                default:
                    break;
                }
			}
			break;
		default:
			break;
		}
	}

private:
	SCHEME_DATA             m_data;

	// 实体指针
	__IEntity				*m_pEntity;

    // 技能ID容器
    vector<int>				m_SpellIDVector;
};
