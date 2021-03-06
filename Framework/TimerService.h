﻿/*******************************************************************
** 文件名:	ITimerService.h
** 版  权:	(C) 深圳冰川网络技术有限公司 2008 - All Rights Reserved
** 创建人:	陈涛 (Carl Chen)
** 日  期:	18/10/2014
** 版  本:	1.0
** 描  述:	时间轴服务

			
********************************************************************/

#pragma once

typedef unsigned long DWORD;
struct ITimerHandler{};

/**
	将时间轴服务再包装一边，以使得在各个线程都能安全调用
*/
struct TimerService
{
		/**
		@purpose          : 设置一个定时器
		@param	 timerID  : 定时器ID
		@param   interval : 定时器调用间隔
		@param   handler  : 处理接口
		@param   callTimes: 调用次数,默认调用无穷次
		@param   debugInfo: 调试信息
		@return		      : 如果设置成功则返回true
		*/
		virtual bool SetTimer( DWORD timerID,DWORD interval,ITimerHandler * handler,DWORD callTimes=INFINITY_CALL,const char * debugInfo=0 ) = 0;

		/**
		@purpose          : 删除定时器
		@param	 timerID  : 定时器ID
		@param   handler  : 处理接口
		@return		      : 返回是否删除成功
		*/
		virtual bool KillTimer( DWORD timerID,ITimerHandler * handler ) = 0;

		virtual void release() = 0;
};
