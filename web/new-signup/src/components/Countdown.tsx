import { useEffect, useState } from "react";

export default function Countdown({
  seconds,
  children,
  message,
  done,
}: React.PropsWithChildren<{
  seconds: number;
  message: (c: number) => string;
  done?: () => void;
}>) {
  const [count, setCount] = useState(seconds);
  useEffect(() => {
    const interval = setInterval(() => {
      setCount((c) => c - 1);
    }, 1000);
    return () => {
      setCount(seconds);
      done?.();
      clearInterval(interval);
    };
  }, [done, seconds]);
  return <div>{count > 0 ? message(count) : children}</div>;
}
