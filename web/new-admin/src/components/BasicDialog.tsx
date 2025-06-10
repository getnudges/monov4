"use client";

import { motion, AnimatePresence } from "framer-motion";

import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";

type BasicDialogProps = {
  title: string;
  message: string;
  open: boolean;
};

export default function BasicDialog({
  title,
  message,
  open = true,
}: BasicDialogProps) {
  return (
    <AlertDialog open={open}>
      <AnimatePresence>
        {open && (
          <AlertDialogContent forceMount asChild>
            <motion.div
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.95 }}
              transition={{ duration: 0.2 }}
            >
              <AlertDialogHeader>
                <AlertDialogTitle>{title}</AlertDialogTitle>
                <AlertDialogDescription>{message}</AlertDialogDescription>
              </AlertDialogHeader>
            </motion.div>
          </AlertDialogContent>
        )}
      </AnimatePresence>
    </AlertDialog>
  );
}
