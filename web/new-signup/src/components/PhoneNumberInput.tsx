import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input, type InputProps } from "@/components/ui/input";
import {
  FieldValues,
  UseControllerProps,
  useFormContext,
} from "react-hook-form";
import { useMask } from "@react-input/mask";

type TextInputProps<T extends FieldValues> = InputProps &
  UseControllerProps<T> & {
    label?: string;
  };

const TextInput = <T extends FieldValues>({
  name,
  label,
  placeholder,
  ...rest
}: TextInputProps<T>) => {
  const { control } = useFormContext<T>();
  const inputRef = useMask({
    mask: "+1 (___) ___-____",
    replacement: { _: /\d/ },
  });
  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem>
          {label && <FormLabel>{label}</FormLabel>}
          <FormControl>
            <Input
              placeholder={placeholder}
              {...rest}
              {...field}
              ref={inputRef}
            />
          </FormControl>
          <FormMessage />
        </FormItem>
      )}
    />
  );
};

export default TextInput;
