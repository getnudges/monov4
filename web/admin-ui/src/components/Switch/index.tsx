import { Switch } from "radix-ui";
import { Form } from "radix-ui";
import {
  Controller,
  type FieldValues,
  type UseControllerProps,
  useFormContext,
} from "react-hook-form";

type SwitchInputProps<T extends FieldValues> = UseControllerProps<T> & {
  label: string;
};

const SwitchInput = <T extends FieldValues>({
  name,
  label,
  ...rest
}: SwitchInputProps<T>) => {
  const { control } = useFormContext<T>();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <Form.Field name={field.name}>
          <Form.Label className="Label">{label}</Form.Label>
          <Form.Control asChild>
            <Switch.Root {...rest} {...field} className="SwitchRoot">
              <Switch.Thumb className="SwitchThumb" />
            </Switch.Root>
          </Form.Control>
          <Form.Message />
        </Form.Field>
      )}
    />
  );
};

export default SwitchInput;
