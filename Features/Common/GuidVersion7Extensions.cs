namespace System;

public static class GuidVersion7Extensions
{
    extension(Guid)
    {
        public static Guid CreateGuid7() => Guid.CreateVersion7();
    }
}
