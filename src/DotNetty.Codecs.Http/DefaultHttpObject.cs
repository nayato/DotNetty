// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {
    using System;

    public class DefaultHttpObject : HttpObject {

    private static readonly int HASH_CODE_PRIME = 31;
    private DecoderResult decoderResult = DecoderResult.SUCCESS;

    protected DefaultHttpObject() {
        // Disallow direct instantiation
    }

    // @Override
    public DecoderResult decoderResult() {
        return decoderResult;
    }

    // @Override
    [Obsolete]
    public DecoderResult getDecoderResult() {
        return decoderResult();
    }

    // @Override
    public void setDecoderResult(DecoderResult decoderResult) {
        if (decoderResult == null) {
            throw new ArgumentNullException(nameof(decoderResult));
        }
        this.decoderResult = decoderResult;
    }

    // @Override
    public override int GetHashCode() {
        int result = 1;
        result = HASH_CODE_PRIME * result + decoderResult.GetHashCode();
        return result;
    }

    // @Override
    public override bool Equals(object o) {
        if (!(o is DefaultHttpObject)) {
            return false;
        }

        DefaultHttpObject other = (DefaultHttpObject) o;

        return decoderResult().Equals(other.decoderResult());
    }
}
