using MirrorImaged.Natives;

if (args is ["coverage"])
{
    Console.Write(Coverage.Of(typeof(GameNatives)));
    return 0;
}

Console.Error.Write("""
    usage:
      mirror coverage    How many natives are at each trust level, and which are Missing

    """);
return 2;
