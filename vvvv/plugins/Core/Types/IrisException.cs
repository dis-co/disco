using System;
using System.Net;

namespace Iris.Core.Types
{
    public class IrisException : Exception
    {
        public enum Type
        {
            UnknownError,
            ObjectNotFound,
            ObjectExists,
            MalformedRequest,
            Conflict
        }

        public Type ExceptionType { get; private set; }

        public IrisException(IrisException.Type type)
            : base(type.ToString())
        {
            ExceptionType = type;
        }

        public static IrisException FromHttpStatusCode(HttpStatusCode code)
        {
            switch(code)
            {
                case HttpStatusCode.NotFound:
                    return new IrisException(Type.ObjectNotFound);
                case HttpStatusCode.BadRequest:
                    return new IrisException(Type.MalformedRequest);
                case HttpStatusCode.PreconditionFailed:
                    return new IrisException(Type.ObjectExists);
                case HttpStatusCode.Conflict:
                    return new IrisException(Type.Conflict);
                default:
                    return new IrisException(Type.UnknownError);
            }
        }

        public static IrisException Conflict()
        {
            return new IrisException(IrisException.Type.Conflict);
        }

        public static IrisException ObjectExists()
        {
            return new IrisException(IrisException.Type.ObjectExists);
        }

        public static IrisException ObjectNotFound()
        {
            return new IrisException(IrisException.Type.ObjectNotFound);
        }

        public static IrisException MalformedRequest()
        {
            return new IrisException(IrisException.Type.MalformedRequest);
        }

        public static IrisException UnknownError()
        {
            return new IrisException(IrisException.Type.UnknownError);
        }
    }
}

