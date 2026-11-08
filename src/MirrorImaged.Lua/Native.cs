using System.Runtime.InteropServices;

namespace MirrorImaged.Lua;

/// <summary>The C API of the Lua library in <c>native/</c>.</summary>
internal static unsafe partial class Native
{
    const string Lib = "mirrorlua";

    // Loads runtimes/<rid>/native/ next to the assembly, where a project reference puts it; a package's copy
    // is found by .NET itself.
    static Native() => NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, (name, assembly, _) =>
    {
        if (name != Lib) return 0;
        var (os, file) = OperatingSystem.IsWindows() ? ("win", "mirrorlua.dll")
            : OperatingSystem.IsMacOS() ? ("osx", "libmirrorlua.dylib") : ("linux", "libmirrorlua.so");
        var rid = $"{os}-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}";
        var dir = Path.GetDirectoryName(assembly.Location) is { Length: > 0 } d ? d : AppContext.BaseDirectory;
        var path = Path.Combine(dir, "runtimes", rid, "native", file);
        return NativeLibrary.TryLoad(path, out var handle) ? handle : 0;
    });

    public const int RegistryIndex = -1000000 - 1000;
    public const int MultipleResults = -1;

    /// <summary>A value crossing to C (sandbox.c's <c>Value</c>); the string is not owned.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Value
    {
        public const int Nil = 0, Boolean = 1, Integer = 2, Float = 3, String = 4;

        public int Type;
        public int IntegerValue;
        public float FloatValue;
        public byte* StringValue;
        public nuint Length;
    }

    [LibraryImport(Lib)] public static partial nint mirror_newstate();
    [LibraryImport(Lib)] public static partial void mirror_close(nint l);
    [LibraryImport(Lib)] public static partial nuint mirror_committed(nint l);
    [LibraryImport(Lib)] public static partial void mirror_setcallback(delegate* unmanaged<nint, Value**, int> f);
    [LibraryImport(Lib)] public static partial int mirror_pcall(nint l, int args, int results);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int mirror_call(nint l, string name, Value* args, int n);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int mirror_getglobal(nint l, string name);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int mirror_setglobal(nint l, string name, Value* value);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int mirror_register(nint l, string name, nint state, int id);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int luaL_loadbufferx(nint l, byte* code, nuint size, string name, string mode);

    [LibraryImport(Lib)] public static partial int lua_gettop(nint l);
    [LibraryImport(Lib)] public static partial void lua_settop(nint l, int index);
    [LibraryImport(Lib)] public static partial int lua_type(nint l, int index);
    [LibraryImport(Lib)] public static partial int lua_toboolean(nint l, int index);
    [LibraryImport(Lib)] public static partial int lua_isinteger(nint l, int index);
    [LibraryImport(Lib)] public static partial int lua_tointegerx(nint l, int index, int* isNumber);
    [LibraryImport(Lib)] public static partial float lua_tonumberx(nint l, int index, int* isNumber);
    [LibraryImport(Lib)] public static partial byte* lua_tolstring(nint l, int index, nuint* length);
    [LibraryImport(Lib)] public static partial nint lua_touserdata(nint l, int index);
}
