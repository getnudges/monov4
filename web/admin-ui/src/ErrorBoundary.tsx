import { Component, type ErrorInfo, type ReactNode } from "react";

type Props = {
  children?: ReactNode;
};

type State = {
  error: string;
};

class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    error: "",
  };

  public static getDerivedStateFromError(error: Error): State {
    // Update state so the next render will show the fallback UI.
    return { error: error.message };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error("Uncaught error:", error, errorInfo);
  }

  public render() {
    if (this.state.error) {
      return (
        <>
          <h1>Sorry... there was an error.</h1>
          <p>Something went wrong. Please try again later.</p>
          <pre>{this.state.error}</pre>
        </>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
