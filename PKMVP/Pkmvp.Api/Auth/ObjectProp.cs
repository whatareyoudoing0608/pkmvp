using System;
using System.Collections.Generic;
using System.Reflection;

namespace Pkmvp.Api.Auth
{
    public static class ObjectProp
    {
        public static void SetUserId(object target, string propName, long userId)
        {
            if (target == null) return;

            var p = target.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p == null || !p.CanWrite) return;

            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

            object v;
            if (t == typeof(long)) v = userId;
            else if (t == typeof(int)) v = (int)userId;
            else if (t == typeof(decimal)) v = (decimal)userId;
            else if (t == typeof(string)) v = userId.ToString();
            else v = Convert.ChangeType(userId, t);

            p.SetValue(target, v, null);
        }

        public static long? GetLong(object obj, params string[] names)
        {
            if (obj == null) return null;

            // Dapper가 dictionary로 주는 케이스
            var dict = obj as IDictionary<string, object>;
            if (dict != null)
            {
                foreach (var n in names)
                {
                    foreach (var k in dict.Keys)
                    {
                        if (string.Equals(k, n, StringComparison.OrdinalIgnoreCase))
                        {
                            var val = dict[k];
                            if (val == null) continue;
                            try { return Convert.ToInt64(val); } catch { }
                        }
                    }
                }
            }

            // 일반 객체(모델/익명타입)
            var type = obj.GetType();
            foreach (var n in names)
            {
                var p = type.GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p == null) continue;

                var val = p.GetValue(obj, null);
                if (val == null) continue;

                try { return Convert.ToInt64(val); } catch { }
            }

            return null;
        }
    }
}