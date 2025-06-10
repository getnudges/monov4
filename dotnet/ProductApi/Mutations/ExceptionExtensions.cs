namespace ProductApi;

public static class ExceptionExtensions {
    public static Exception GetDeepestInnerException(this Exception exception) =>
        exception.InnerException is null ? exception : GetDeepestInnerException(exception.InnerException);
}
