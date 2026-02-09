import * as React from "react"

import { cn } from "@/lib/utils"

// --- Context ---
type InputOTPContextValue = {
  value: string
  activeIndex: number
  maxLength: number
  focus: () => void
}

const InputOTPContext = React.createContext<InputOTPContextValue>({
  value: "",
  activeIndex: -1,
  maxLength: 6,
  focus: () => {},
})

// --- InputOTP (root) ---
interface InputOTPProps
  extends Omit<React.InputHTMLAttributes<HTMLInputElement>, "onChange" | "value"> {
  maxLength: number
  value?: string
  onChange?: (value: string) => void
  containerClassName?: string
}

const InputOTP = React.forwardRef<HTMLInputElement, InputOTPProps>(
  (
    {
      maxLength,
      value = "",
      onChange,
      onBlur,
      containerClassName,
      className,
      children,
      ...props
    },
    ref
  ) => {
    const internalRef = React.useRef<HTMLInputElement>(null)
    const inputRef = (ref as React.RefObject<HTMLInputElement>) || internalRef
    const [isFocused, setIsFocused] = React.useState(false)

    const activeIndex = isFocused ? Math.min(value.length, maxLength - 1) : -1

    const focus = React.useCallback(() => {
      inputRef.current?.focus()
    }, [inputRef])

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      const newValue = e.target.value.replace(/[^0-9]/g, "").slice(0, maxLength)
      onChange?.(newValue)
    }

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === "Backspace" && value.length > 0) {
        e.preventDefault()
        onChange?.(value.slice(0, -1))
      }
    }

    const handlePaste = (e: React.ClipboardEvent<HTMLInputElement>) => {
      e.preventDefault()
      const pasted = e.clipboardData
        .getData("text/plain")
        .replace(/[^0-9]/g, "")
        .slice(0, maxLength)
      onChange?.(pasted)
    }

    const handleFocus = () => setIsFocused(true)

    const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
      setIsFocused(false)
      onBlur?.(e)
    }

    const ctx = React.useMemo(
      () => ({ value, activeIndex, maxLength, focus }),
      [value, activeIndex, maxLength, focus]
    )

    return (
      <InputOTPContext.Provider value={ctx}>
        <div
          className={cn(
            "flex items-center gap-2 has-[:disabled]:opacity-50",
            containerClassName
          )}
          onClick={focus}
        >
          <input
            ref={inputRef}
            inputMode="numeric"
            autoComplete="one-time-code"
            pattern="[0-9]*"
            value={value}
            onChange={handleChange}
            onKeyDown={handleKeyDown}
            onPaste={handlePaste}
            onFocus={handleFocus}
            onBlur={handleBlur}
            maxLength={maxLength}
            className={cn(
              "sr-only absolute",
              "disabled:cursor-not-allowed",
              className
            )}
            {...props}
          />
          {children}
        </div>
      </InputOTPContext.Provider>
    )
  }
)
InputOTP.displayName = "InputOTP"

// --- InputOTPGroup ---
const InputOTPGroup = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div ref={ref} className={cn("flex items-center", className)} {...props} />
))
InputOTPGroup.displayName = "InputOTPGroup"

// --- InputOTPSlot ---
const InputOTPSlot = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement> & { index: number }
>(({ index, className, ...props }, ref) => {
  const { value, activeIndex } = React.useContext(InputOTPContext)
  const char = value[index] ?? ""
  const isActive = index === activeIndex
  const hasFakeCaret = isActive && char === ""

  return (
    <div
      ref={ref}
      className={cn(
        "relative flex h-10 w-10 items-center justify-center border-y border-r border-input text-sm transition-all first:rounded-l-md first:border-l last:rounded-r-md",
        isActive && "z-10 ring-2 ring-ring ring-offset-background",
        className
      )}
      {...props}
    >
      {char}
      {hasFakeCaret && (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
          <div className="h-4 w-px animate-caret-blink bg-foreground duration-1000" />
        </div>
      )}
    </div>
  )
})
InputOTPSlot.displayName = "InputOTPSlot"

// --- InputOTPSeparator ---
const InputOTPSeparator = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ ...props }, ref) => (
  <div ref={ref} role="separator" {...props}>
    <svg width="8" height="8" viewBox="0 0 8 8" fill="currentColor">
      <circle cx="4" cy="4" r="2" />
    </svg>
  </div>
))
InputOTPSeparator.displayName = "InputOTPSeparator"

export { InputOTP, InputOTPGroup, InputOTPSlot, InputOTPSeparator }
