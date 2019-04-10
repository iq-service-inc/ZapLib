﻿using System;

namespace ZapLib.Utility
{
    /// <summary>
    /// 萬用的型態輔助工具
    /// </summary>
    public static class Cast
    {
        /// <summary>
        /// 指定類別 T 判斷參數中的物件是否為該類別或衍生自該類別
        /// </summary>
        /// <typeparam name="T">指定類別</typeparam>
        /// <param name="obj">要判斷的物件</param>
        /// <returns>如果同類別將回傳 True 否則 False</returns>
        public static bool IsType<T>(object obj)
        {
            return obj == null ? CanBeNull(typeof(T)) :
                (obj is T || typeof(T).IsAssignableFrom(obj.GetType()));
        }

        /// <summary>
        /// 指定兩個物件判斷是否為同類別或衍生自同樣類別
        /// </summary>
        /// <param name="obj1">第一個要判斷的物件</param>
        /// <param name="obj2">第二個要判斷的物件</param>
        /// <returns>如果同類別將回傳 True 否則 False</returns>
        public static bool IsType(object obj1, object obj2)
        {
            if (obj1 == null && obj2 == null) return true;
            else if (obj1 != null && obj2 == null) return CanBeNull(obj1.GetType());
            else if (obj1 == null && obj2 != null) return CanBeNull(obj2.GetType());
            else
            {
                Type t1 = obj1.GetType();
                Type t2 = obj2.GetType();
                return (t1.Equals(t2) || t2.IsAssignableFrom(t1) || t1.IsAssignableFrom(t2));
            }
        }

        /// <summary>
        /// 判斷類別 T 是否可以被指定為 NULL
        /// </summary>
        /// <param name="t">指定類別</param>
        /// <returns>如果可以被指定為 NULL 將回傳 True 否則 False</returns>
        public static bool CanBeNull(Type t) => !t.IsValueType || (Nullable.GetUnderlyingType(t) != null);


        /// <summary>
        /// 將參數中的物件轉換成明確型態 T，轉換不過則回傳型態 T 的預設值
        /// </summary>
        /// <typeparam name="T">指定型態</typeparam>
        /// <param name="obj">要轉換的物件</param>
        /// <param name="def_val">指定預設值</param>
        /// <returns>轉換過後的數值，如果轉換不過則回傳預設值</returns>
        public static T To<T>(object obj, T def_val = default)
        {
            try
            {
                return (T)Convert.ChangeType(obj, typeof(T));
            }
            catch
            {
                return def_val;
            }
        }

        /// <summary>
        /// 將參數中的物件轉換成指定型態，轉換不過則回傳該型態的預設值
        /// </summary>
        /// <param name="obj">要轉換的物件</param>
        /// <param name="targetType">指定型態</param>
        /// <returns>轉換過後的數值，如果轉換不過則回傳預設值</returns>
        public static object To(object obj, Type targetType)
        {
            try
            {
                return Convert.ChangeType(obj, targetType);
            }
            catch
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }
    }
}