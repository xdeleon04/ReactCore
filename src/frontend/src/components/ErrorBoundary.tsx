import React from 'react';

type Props = {
  children: React.ReactNode;
};

type State = {
  hasError: boolean;
};

export class ErrorBoundary extends React.Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  componentDidCatch(error: unknown) {
    console.error(error);
  }

  render() {
    if (this.state.hasError) {
      return (
        <main className="mx-auto w-full max-w-4xl p-4">
          <div className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
            Something went wrong. Please refresh and try again.
          </div>
        </main>
      );
    }

    return this.props.children;
  }
}
