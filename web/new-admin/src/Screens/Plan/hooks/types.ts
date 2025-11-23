export type GraphQLSubscriptionError = {
  message: string;
  extensions?: {
    message: string;
    stackTrace?: string;
  };
};
