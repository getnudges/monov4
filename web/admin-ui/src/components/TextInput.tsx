import { Form } from "radix-ui";
import {
  Controller,
  type FieldValues,
  type UseControllerProps,
  useFormContext,
} from "react-hook-form";

type TextInputProps<T extends FieldValues> = UseControllerProps<T> & {
  label: string;
  placeholder?: string;
};

const TextInput = <T extends FieldValues>({
  name,
  label,
  placeholder,
  ...rest
}: TextInputProps<T>) => {
  const { control } = useFormContext<T>();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <Form.Field name={field.name}>
          <Form.Label>{label}</Form.Label>
          <Form.Control asChild>
            <input placeholder={placeholder} {...rest} {...field} />
          </Form.Control>
          <Form.Message />
        </Form.Field>
      )}
    />
  );
};

export default TextInput;
