"use client";

import {
  useState,
  useEffect,
  createContext,
  PropsWithChildren,
  useContext,
} from "react";
import { X, CheckCircle, AlertCircle, AlertTriangle } from "lucide-react";

type SnackbarContextType = {
  showSnackbar: (
    variant: SnackbarVariant,
    message: string,
    timeout: number
  ) => void;
};

const SnackbarContext = createContext<SnackbarContextType>(
  {} as SnackbarContextType
);

type SnackbarVariant = "success" | "error" | "warning";

export const useSnackbar = () => {
  const context = useContext(SnackbarContext);
  if (!context) {
    throw new Error("useSnackbar must be used within a SnackbarProvider");
  }
  return context;
};

export const SnackbarProvider: React.FC<PropsWithChildren> = ({ children }) => {
  const [snackbarMessage, setSnackbarMessage] = useState("");
  const [snackbarTimeout, setSnackbarTimeout] = useState(3000);
  const [variant, setVariant] = useState<SnackbarVariant>("success");

  const showSnackbar = (
    variant: SnackbarVariant,
    message: string,
    timeout: number = 3000
  ) => {
    setSnackbarMessage(message);
    setSnackbarTimeout(timeout);
    setVariant(variant);
  };

  useEffect(() => {
    if (snackbarMessage) {
      setTimeout(() => {
        setSnackbarMessage("");
      }, snackbarTimeout);
    }
  }, [snackbarMessage, snackbarTimeout]);

  return (
    <SnackbarContext.Provider value={{ showSnackbar }}>
      {children}
      {snackbarMessage && (
        <Snackbar
          message={snackbarMessage}
          duration={snackbarTimeout}
          variant={variant}
          onClose={() => setSnackbarMessage("")}
        />
      )}
    </SnackbarContext.Provider>
  );
};

interface SnackbarProps {
  message: string;
  duration?: number;
  onClose?: () => void;
  variant: SnackbarVariant;
}

export default function Snackbar({
  message,
  duration = 3000,
  onClose,
  variant,
}: SnackbarProps) {
  useEffect(() => {
    const timer = setTimeout(() => {
      onClose?.();
    }, duration);

    return () => clearTimeout(timer);
  }, [duration, onClose]);

  const getVariantStyles = (variant: SnackbarVariant) => {
    switch (variant) {
      case "success":
        return "bg-green-100 dark:bg-green-800 text-green-800 dark:text-green-100 border-green-300 dark:border-green-700";
      case "error":
        return "bg-red-100 dark:bg-red-800 text-red-800 dark:text-red-100 border-red-300 dark:border-red-700";
      case "warning":
        return "bg-yellow-100 dark:bg-yellow-800 text-yellow-800 dark:text-yellow-100 border-yellow-300 dark:border-yellow-700";
    }
  };

  const getIcon = (variant: SnackbarVariant) => {
    switch (variant) {
      case "success":
        return <CheckCircle className="w-5 h-5 mr-2" />;
      case "error":
        return <AlertCircle className="w-5 h-5 mr-2" />;
      case "warning":
        return <AlertTriangle className="w-5 h-5 mr-2" />;
    }
  };

  return (
    <div className="fixed bottom-4 left-4 right-4 md:left-auto md:right-4 md:w-96 z-50">
      <div
        className={`rounded-lg shadow-lg p-4 flex items-center justify-between transition-all duration-300 ease-in-out ${getVariantStyles(
          variant
        )}`}
      >
        {getIcon(variant)}
        <p className="text-sm">{message}</p>
        <button
          onClick={() => {
            onClose?.();
          }}
          className="ml-4 text-current opacity-70 hover:opacity-100 transition-opacity duration-200"
        >
          <X size={18} />
        </button>
      </div>
    </div>
  );
}
