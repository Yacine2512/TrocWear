import { HttpInterceptorFn } from "@angular/common/http";
export const sessionInterceptor: HttpInterceptorFn = (req, next) => {
  const mutation = ["POST", "PATCH", "PUT", "DELETE"].includes(req.method);
  return next(
    req.clone({
      withCredentials: true,
      setHeaders: mutation ? { "X-TrocWear-Request": "1" } : {},
    }),
  );
};
