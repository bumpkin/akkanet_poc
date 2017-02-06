using System;

namespace AkkaNet.Poc.Core.Placeholder
{
    public interface IExceptionTyper
    {
        bool IsTransientException(Exception exception);
    }
}