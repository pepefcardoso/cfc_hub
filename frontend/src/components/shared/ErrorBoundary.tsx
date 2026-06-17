import React, { Component, ErrorInfo, ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { AlertCircle } from "lucide-react";

interface Props {
  children?: ReactNode;
}

interface State {
  hasError: boolean;
  error?: Error;
}

export class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error("Uncaught error:", error, errorInfo);
  }

  private handleRetry = () => {
    this.setState({ hasError: false, error: undefined });
  };

  public render() {
    if (this.state.hasError) {
      return (
        <div className="flex flex-col items-center justify-center p-8 text-center rounded-lg border border-cfc-status-error/30 bg-cfc-status-error/5 min-h-[300px]">
          <div className="bg-cfc-surface-100 p-3 rounded-full shadow-sm mb-4 border border-cfc-status-error/20">
            <AlertCircle className="h-8 w-8 text-cfc-status-error" />
          </div>
          <h2 className="text-xl font-semibold text-cfc-text-primary mb-2">
            Ocorreu um erro inesperado
          </h2>
          <p className="text-sm text-cfc-text-secondary max-w-md mb-6">
            Não foi possível carregar esta parte da página. Nossa equipe foi notificada do problema.
          </p>
          <Button 
            onClick={this.handleRetry}
            variant="outline"
            className="border-cfc-status-error text-cfc-status-error hover:bg-cfc-status-error/10"
          >
            Tentar novamente
          </Button>
        </div>
      );
    }

    return this.props.children;
  }
}
