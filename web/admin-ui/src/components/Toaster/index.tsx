import * as React from "react";
import { Toast } from "radix-ui";
import "./styles.css";

type ToasterContextType = {
  notify: (message: string, title: string, duration?: number) => void;
};

const ToasterContext = React.createContext<ToasterContextType>(
  {} as ToasterContextType
);

export const useToaster = () => {
  return React.useContext(ToasterContext);
};

const ToasterProvider: React.FC<React.PropsWithChildren<{}>> = ({
  children,
}) => {
  const [open, setOpen] = React.useState(false);
  const [message, setMessage] = React.useState("");
  const [title, setTitle] = React.useState("");
  const timerRef = React.useRef(0);

  React.useEffect(() => {
    return () => clearTimeout(timerRef.current);
  }, []);

  const notify = (message: string, title: string, duration?: number) => {
    setOpen(false);
    setMessage(message);
    setTitle(title);
    window.clearTimeout(timerRef.current);
    timerRef.current = window.setTimeout(() => {
      setOpen(true);
    }, duration ?? 2500);
  };

  return (
    <ToasterContext.Provider value={{ notify }}>
      {children}
      <Toast.Provider swipeDirection="right">
        <Toast.Root className="ToastRoot" open={open} onOpenChange={setOpen}>
          <Toast.Title className="ToastTitle">{title}</Toast.Title>
          <Toast.Description asChild>
            <p className="ToastDescription">{message}</p>
          </Toast.Description>
        </Toast.Root>
        <Toast.Viewport className="ToastViewport" />
      </Toast.Provider>
    </ToasterContext.Provider>
  );
};

export default ToasterProvider;
