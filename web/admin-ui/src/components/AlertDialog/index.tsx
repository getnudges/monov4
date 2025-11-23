import { AlertDialog } from "radix-ui";

type AlertProps = {
  title: string;
  message: string;
  actionText?: string;
  open?: boolean;
  onClose?: () => void;
};

const Alert = ({
  open,
  title,
  message,
  actionText = "Close",
  onClose,
}: AlertProps) => {
  const handleClose = () => {
    onClose?.();
  };
  return (
    <AlertDialog.Root open={open}>
      <AlertDialog.Portal>
        <AlertDialog.Overlay className="AlertDialogOverlay" />
        <AlertDialog.Content className="AlertDialogContent">
          <AlertDialog.Title className="AlertDialogTitle">
            {title}
          </AlertDialog.Title>
          <AlertDialog.Description className="AlertDialogDescription">
            {message}
          </AlertDialog.Description>
          <div style={{ display: "flex", gap: 25, justifyContent: "flex-end" }}>
            <AlertDialog.Cancel asChild>
              <button className="Button mauve">Cancel</button>
            </AlertDialog.Cancel>
            <AlertDialog.Action asChild onClick={handleClose}>
              <button className="Button red">{actionText}</button>
            </AlertDialog.Action>
          </div>
        </AlertDialog.Content>
      </AlertDialog.Portal>
    </AlertDialog.Root>
  );
};

export default Alert;
