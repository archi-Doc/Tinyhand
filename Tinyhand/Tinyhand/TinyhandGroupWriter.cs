// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.IO;

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1202
#pragma warning disable SA1401 // Fields should be private

namespace Tinyhand;

public ref struct TinyhandGroupWriter
{
    public readonly TinyhandComposeOption ComposeOption;

    private int indents;
    private int firstSerial;
    private int secondSerial;
    private int lfCount;

    public bool EnableIndent => this.ComposeOption == TinyhandComposeOption.Standard || this.ComposeOption == TinyhandComposeOption.UseContextualInformation;

    public int Indents => this.indents;

    public TinyhandGroupWriter(TinyhandComposeOption composeOption)
    {
        this.ComposeOption = composeOption;
    }

    public void AddLF()
    {
        this.lfCount++;
    }

    public void ProcessStartGroup(ref TinyhandRawWriter writer)
    {
        if (!this.EnableIndent)
        {
            writer.WriteUInt8(TinyhandConstants.OpenBrace);
            return;
        }

ProcessPartialLoop:
        if (this.firstSerial == 0)
        {// this.secondSerial == 0
            this.firstSerial++;
        }
        else if (this.firstSerial < 0)
        {// this.secondSerial >= 0
            this.secondSerial++;
        }
        else
        {// this.firstSerial > 0
            if (this.secondSerial == 0)
            {
                this.firstSerial++;
            }
            else
            {// this.secondSerial < 0
                this.ProcessPartial(ref writer);
                goto ProcessPartialLoop;
            }
        }
    }

    public void ProcessEndGroup(ref TinyhandRawWriter writer)
    {
        if (!this.EnableIndent)
        {
            writer.WriteUInt8(TinyhandConstants.CloseBrace);
            return;
        }

ProcessPartialLoop:
        if (this.firstSerial == 0)
        {// this.secondSerial == 0
            this.firstSerial--;
        }
        else if (this.firstSerial < 0)
        {// this.secondSerial >= 0
            if (this.secondSerial == 0)
            {
                this.firstSerial--;
            }
            else
            {// this.secondSerial > 0
                this.ProcessPartial(ref writer);
                goto ProcessPartialLoop;
            }
        }
        else
        {// this.firstSerial > 0
            this.secondSerial--;
        }
    }

    public void Flush(ref TinyhandRawWriter writer)
    {
        if (this.lfCount > 0)
        {
            writer.WriteLF();
            writer.WriteSpan(TinyhandTreeConverter.GetIndentSpan(this.indents));
            this.lfCount = 0;
        }

        if (this.firstSerial == 0)
        {// 0 serial
            return;
        }
        else if (this.secondSerial == 0)
        {// 1 serial
            this.indents += this.firstSerial;
            writer.WriteLF();
            if (this.firstSerial > 1)
            { // {{{ -> LF+4 "+ "
                writer.WriteSpan(TinyhandTreeConverter.GetIndentSpan(this.indents - 1));
                writer.WriteUInt16(TinyhandConstants.StartGroup);
            }
            else
            {
                writer.WriteSpan(TinyhandTreeConverter.GetIndentSpan(this.indents));
            }

            this.firstSerial = 0;
            return;
        }
        else if (this.firstSerial > 0)
        {// 2serials: 1st '{' 2nd '}'
            if (this.firstSerial >= -this.secondSerial)
            {// 3, -2: {{{}}
                var dif = this.firstSerial + this.secondSerial;
                if (dif != 0)
                {
                    this.indents += dif;
                    writer.WriteLF();
                    writer.WriteSpan(TinyhandTreeConverter.GetIndentSpan(this.indents));
                }

                for (var i = 0; i < -this.secondSerial; i++)
                {
                    writer.WriteUInt8(TinyhandConstants.OpenBrace);
                    writer.WriteUInt8(TinyhandConstants.CloseBrace);
                }

                this.AddLF();
            }
            else
            {// 2, -3: {{}}}
                for (var i = 0; i < this.firstSerial; i++)
                {
                    writer.WriteUInt8(TinyhandConstants.OpenBrace);
                    writer.WriteUInt8(TinyhandConstants.CloseBrace);
                }

                this.indents += this.firstSerial + this.secondSerial;
                writer.WriteLF();
                writer.WriteSpan(TinyhandTreeConverter.GetIndentSpan(this.indents));
            }
        }
        else
        {// 2serials: 1st '}' 2nd '{'
            if (-this.firstSerial >= this.secondSerial)
            {// -3, 2: }}}{{
                this.indents += this.firstSerial;
                writer.WriteLF();
                writer.WriteSpan(TinyhandTreeConverter.GetIndentSpan(this.indents));
                for (var i = 0; i < this.secondSerial; i++)
                {
                    writer.WriteUInt16(TinyhandConstants.StartGroup);
                }

                this.indents += this.secondSerial;
            }
            else
            {// -2, 3: }}{{{
                this.indents += this.firstSerial;
                writer.WriteLF();
                writer.WriteSpan(TinyhandTreeConverter.GetIndentSpan(this.indents));
                for (var i = 0; i < this.secondSerial; i++)
                {
                    writer.WriteUInt16(TinyhandConstants.StartGroup);
                }

                this.indents += this.secondSerial;
            }
        }

        this.firstSerial = 0;
        this.secondSerial = 0;

        if (this.lfCount > 0)
        {
            writer.WriteLF();
            writer.WriteSpan(TinyhandTreeConverter.GetIndentSpan(this.indents));
            this.lfCount = 0;
        }
    }

    private void ProcessPartial(ref TinyhandRawWriter writer)
    {
        if (this.firstSerial > 0)
        {// 2serials: 1st '{' 2nd '}'
            if (this.firstSerial >= -this.secondSerial)
            {// 3, -2: {{{}}
                this.indents += this.firstSerial + this.secondSerial;
                writer.WriteLF();
                writer.WriteSpan(TinyhandTreeConverter.GetIndentSpan(this.indents));
                for (var i = 0; i < -this.secondSerial; i++)
                {
                    writer.WriteUInt8(TinyhandConstants.OpenBrace);
                    writer.WriteUInt8(TinyhandConstants.CloseBrace);
                }

                this.firstSerial = 0;
                this.secondSerial = 0;
            }
            else
            {// 2, -3: {{}}}
                for (var i = 0; i < this.firstSerial; i++)
                {
                    writer.WriteUInt8(TinyhandConstants.OpenBrace);
                    writer.WriteUInt8(TinyhandConstants.CloseBrace);
                }

                this.firstSerial += this.secondSerial;
                this.secondSerial = 0;
            }
        }
        else
        {// 2serials: 1st '}' 2nd '{'
            if (-this.firstSerial >= this.secondSerial)
            {// -3, 2: }}}{{
                this.indents += this.firstSerial;
                writer.WriteLF();
                writer.WriteSpan(TinyhandTreeConverter.GetIndentSpan(this.indents));
                for (var i = 0; i < this.secondSerial; i++)
                {
                    writer.WriteUInt8((byte)'+');
                    writer.WriteUInt8(TinyhandConstants.Space);
                }

                this.indents += this.secondSerial;
                this.firstSerial = 0;
                this.secondSerial = 0;
            }
            else
            {// -2, 3: }}{{{
                this.indents += this.firstSerial;
                writer.WriteLF();
                writer.WriteSpan(TinyhandTreeConverter.GetIndentSpan(this.indents));
                for (var i = 0; i < this.secondSerial; i++)
                {
                    writer.WriteUInt8((byte)'+');
                    writer.WriteUInt8(TinyhandConstants.Space);
                }

                this.indents += this.secondSerial;
                this.firstSerial += this.secondSerial;
                this.secondSerial = 0;
            }
        }
    }
}
