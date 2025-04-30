using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;

namespace PackBuilder.Core.Utils
{
    internal class AutoDetourLoader : ModSystem
    {
        internal abstract class AutoDetour
        {
            public abstract MethodInfo Method { get; }
            public abstract Delegate DetourDelegate { get; }
        }

        public static List<Hook> Detours = [];

        public override void Load()
        {
            var types = Mod.GetType().Assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(AutoDetour)));

            foreach (var type in types)
            {
                var instance = (AutoDetour)Activator.CreateInstance(type);
                Hook hook = new(instance.Method, instance.DetourDelegate);
                Detours.Add(hook);
            }
        }

        public override void Unload()
        {
            Detours.Clear();
        }
    }

    internal abstract class AutoVoidDetour : AutoDetourLoader.AutoDetour
    {
        public sealed override Delegate DetourDelegate => Detour;
        public abstract void Detour(Action orig);
    }

    internal abstract class AutoVoidDetour<T1> : AutoDetourLoader.AutoDetour
    {
        public sealed override Delegate DetourDelegate => Detour;
        public abstract void Detour(Action<T1> orig, T1 arg1);
    }

    internal abstract class AutoVoidDetour<T1, T2> : AutoDetourLoader.AutoDetour
    {
        public sealed override Delegate DetourDelegate => Detour;
        public abstract void Detour(Action<T1, T2> orig, T1 arg1, T2 arg2);
    }

    internal abstract class AutoVoidDetour<T1, T2, T3> : AutoDetourLoader.AutoDetour
    {
        public sealed override Delegate DetourDelegate => Detour;
        public abstract void Detour(Action<T1, T2, T3> orig, T1 arg1, T2 arg2, T3 arg3);
    }

    internal abstract class AutoFuncDetour<TResult> : AutoDetourLoader.AutoDetour
    {
        public sealed override Delegate DetourDelegate => Detour;
        public abstract TResult Detour(Func<TResult> orig);
    }

    internal abstract class AutoFuncDetour<T1, TResult> : AutoDetourLoader.AutoDetour
    {
        public sealed override Delegate DetourDelegate => Detour;
        public abstract TResult Detour(Func<T1, TResult> orig, T1 arg1);
    }

    internal abstract class AutoFuncDetour<T1, T2, TResult> : AutoDetourLoader.AutoDetour
    {
        public sealed override Delegate DetourDelegate => Detour;
        public abstract TResult Detour(Func<T1, T2, TResult> orig, T1 arg1, T2 arg2);
    }
}
