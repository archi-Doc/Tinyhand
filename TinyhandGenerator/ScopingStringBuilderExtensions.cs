// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Visceral;

namespace Tinyhand.Generator
{
    internal static class ScopingStringBuilderExtensions
    {
        internal static ScopingStringBuilder.IScope ScopeSecurityDepth(this ScopingStringBuilder ssb)
        {
            ssb.AppendLine("options.Security.DepthStep(ref reader);");
            return ssb.ScopeBrace("try");
        }

        internal static void RestoreSecurityDepth(this ScopingStringBuilder ssb)
        {
            ssb.AppendLine("finally { reader.Depth--; }");
        }

        internal static void ReaderSkip(this ScopingStringBuilder ssb) => ssb.AppendLine("reader.Skip();");

        internal static void Continue(this ScopingStringBuilder ssb) => ssb.AppendLine("continue;");

        internal static void GotoSkipLabel(this ScopingStringBuilder ssb) => ssb.AppendLine("goto SkipLabel;");
    }
}
