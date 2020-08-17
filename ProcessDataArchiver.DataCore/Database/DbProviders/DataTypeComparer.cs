using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.Database.DbProviders
{
    public static class DataTypeComparer
    {
        public static Type GetEquivalentType(Type oldType, Type newType)
        {
            if(oldType == typeof(bool))
            {
                return newType;
            }
            else if(oldType == typeof(byte))
            {
                if (newType != typeof(bool) && newType!=typeof(sbyte))
                {
                    return newType;
                }
                else if(newType == typeof(sbyte))
                {
                    return typeof(short);
                }
                else
                {
                    return oldType;
                }
            }
            else if(oldType == typeof(sbyte))
            {
                if(newType!=typeof(bool) && newType != typeof(byte))
                {
                    return newType;
                }
                else if(newType == typeof(byte))
                {
                    return typeof(short);
                }
                else
                {
                    return oldType;
                }
            }
            else if(oldType == typeof(short))
            {
                if(newType!=typeof(bool) && newType!=typeof(byte) && newType!=typeof(sbyte) 
                    && newType != typeof(ushort))
                {
                    return newType;
                }
                else if(newType == typeof(ushort))
                {
                    return typeof(int);
                }
                else
                {
                    return oldType;
                }
            }
            else if (oldType == typeof(ushort))
            {
                if (newType != typeof(bool) && newType != typeof(byte) && newType != typeof(sbyte)
                    && newType != typeof(short))
                {
                    return newType;
                }
                else if (newType == typeof(short))
                {
                    return typeof(int);
                }
                else
                {
                    return oldType;
                }
            }
            else if (oldType == typeof(int))
            {
                if (newType != typeof(bool) && newType != typeof(byte) && newType != typeof(sbyte)
                    && newType != typeof(short) && newType != typeof(ushort) && newType != typeof(uint))
                {
                    return newType;
                }
                else if (newType == typeof(uint))
                {
                    return typeof(long);
                }
                else
                {
                    return oldType;
                }
            }
            else if (oldType == typeof(int))
            {
                if (newType != typeof(bool) && newType != typeof(byte) && newType != typeof(sbyte)
                    && newType != typeof(short) && newType != typeof(ushort) && newType != typeof(int))
                {
                    return newType;
                }
                else if (newType == typeof(int))
                {
                    return typeof(long);
                }
                else
                {
                    return oldType;
                }
            }
            else if (oldType == typeof(long))
            {
                if (newType != typeof(bool) && newType != typeof(byte) && newType != typeof(sbyte)
                    && newType != typeof(short) && newType != typeof(ushort) && newType != typeof(int)
                    && newType!=typeof(ulong))
                {
                    return newType;
                }
                else if (newType == typeof(ulong))
                {
                    return typeof(double);
                }
                else
                {
                    return oldType;
                }
            }
            else if (oldType == typeof(ulong))
            {
                if (newType != typeof(bool) && newType != typeof(byte) && newType != typeof(sbyte)
                    && newType != typeof(short) && newType != typeof(ushort) && newType != typeof(int)
                    && newType != typeof(long))
                {
                    return newType;
                }
                else if (newType == typeof(long))
                {
                    return typeof(double);
                }
                else
                {
                    return oldType;
                }
            }
            else if (oldType == typeof(float))
            {
                if(newType == typeof(double))
                {
                    return newType;
                }
                else
                {
                    return oldType;
                }
            }
            else
            {
                return typeof(double);
            }
        }
    }
}
