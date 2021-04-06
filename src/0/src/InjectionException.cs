﻿// Copyright (c) Teroneko.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Teronis.ModuleInitializer.AssemblyLoader
{
    [Serializable]
    internal class InjectionException : Exception
    {
        public InjectionException()
        { }

        public InjectionException(string message)
            : base(message)
        { }

        public InjectionException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected InjectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
