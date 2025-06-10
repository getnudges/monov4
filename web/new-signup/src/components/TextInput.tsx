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

type TextInputProps<T extends FieldValues> = InputProps &
  UseControllerProps<T> & {
    label: string;
  };

const TextInput = <T extends FieldValues>({
  name,
  label,
  placeholder,
  ...rest
}: TextInputProps<T>) => {
  const { control } = useFormContext<T>();
  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem>
          <FormLabel>{label}</FormLabel>
          <FormControl>
            <Input placeholder={placeholder} {...rest} {...field} />
          </FormControl>
          <FormMessage />
        </FormItem>
      )}
    />
  );
};

export default TextInput;
