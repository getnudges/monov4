# Components

Component library documentation for the new-admin application.

## Table of Contents

- [Overview](#overview)
- [shadcn/ui Components](#shadcnui-components)
- [Custom Components](#custom-components)
- [Layout Components](#layout-components)
- [Utility Functions](#utility-functions)

## Overview

The app uses a two-tier component system:

1. **shadcn/ui components** (`src/components/ui/`): Unstyled, accessible primitives from Radix UI
2. **Custom components** (`src/components/`): App-specific components built on top of shadcn/ui

All components are fully typed with TypeScript and support dark mode via CSS variables.

## shadcn/ui Components

Pre-installed shadcn/ui components available in `src/components/ui/`.

### Button

**Import:**
```typescript
import { Button } from "@/components/ui/button";
```

**Variants:**
- `default` - Primary button (filled)
- `secondary` - Secondary style
- `outline` - Outlined button
- `ghost` - Transparent button
- `link` - Link-styled button
- `destructive` - Danger/delete actions

**Sizes:**
- `default` - Standard size
- `sm` - Small
- `lg` - Large
- `icon` - Square icon button

**Example:**
```typescript
<Button variant="default" size="default">
  Click me
</Button>

<Button variant="outline" size="sm">
  Cancel
</Button>

<Button variant="destructive">
  Delete
</Button>
```

### Card

**Import:**
```typescript
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  CardFooter,
} from "@/components/ui/card";
```

**Example:**
```typescript
<Card>
  <CardHeader>
    <CardTitle>Card Title</CardTitle>
    <CardDescription>Optional description</CardDescription>
  </CardHeader>
  <CardContent>
    <p>Card content goes here</p>
  </CardContent>
  <CardFooter>
    <Button>Action</Button>
  </CardFooter>
</Card>
```

### Form Components

**Import:**
```typescript
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
```

**Example with React Hook Form:**
```typescript
<Form {...form}>
  <form onSubmit={form.handleSubmit(onSubmit)}>
    <FormField
      control={form.control}
      name="name"
      render={({ field }) => (
        <FormItem>
          <FormLabel>Name</FormLabel>
          <FormControl>
            <Input placeholder="Enter name" {...field} />
          </FormControl>
          <FormDescription>Your display name</FormDescription>
          <FormMessage />  {/* Shows validation errors */}
        </FormItem>
      )}
    />

    <FormField
      control={form.control}
      name="description"
      render={({ field }) => (
        <FormItem>
          <FormLabel>Description</FormLabel>
          <FormControl>
            <Textarea placeholder="Enter description" {...field} />
          </FormControl>
          <FormMessage />
        </FormItem>
      )}
    />

    <FormField
      control={form.control}
      name="enabled"
      render={({ field }) => (
        <FormItem>
          <FormLabel>Enabled</FormLabel>
          <FormControl>
            <Switch checked={field.value} onCheckedChange={field.onChange} />
          </FormControl>
        </FormItem>
      )}
    />

    <Button type="submit">Submit</Button>
  </form>
</Form>
```

### Select

**Import:**
```typescript
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
```

**Example:**
```typescript
<Select onValueChange={(value) => console.log(value)}>
  <SelectTrigger>
    <SelectValue placeholder="Select option" />
  </SelectTrigger>
  <SelectContent>
    <SelectItem value="option1">Option 1</SelectItem>
    <SelectItem value="option2">Option 2</SelectItem>
    <SelectItem value="option3">Option 3</SelectItem>
  </SelectContent>
</Select>
```

**With React Hook Form:**
```typescript
<FormField
  control={form.control}
  name="type"
  render={({ field }) => (
    <FormItem>
      <FormLabel>Type</FormLabel>
      <Select onValueChange={field.onChange} defaultValue={field.value}>
        <FormControl>
          <SelectTrigger>
            <SelectValue placeholder="Select type" />
          </SelectTrigger>
        </FormControl>
        <SelectContent>
          <SelectItem value="type1">Type 1</SelectItem>
          <SelectItem value="type2">Type 2</SelectItem>
        </SelectContent>
      </Select>
      <FormMessage />
    </FormItem>
  )}
/>
```

### Alert & AlertDialog

**Import:**
```typescript
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
```

**Alert (non-modal):**
```typescript
<Alert variant="default">
  <AlertTitle>Heads up!</AlertTitle>
  <AlertDescription>
    You can add components to your app using the cli.
  </AlertDescription>
</Alert>

<Alert variant="destructive">
  <AlertTitle>Error</AlertTitle>
  <AlertDescription>Something went wrong.</AlertDescription>
</Alert>
```

**AlertDialog (modal):**
```typescript
<AlertDialog>
  <AlertDialogTrigger asChild>
    <Button variant="destructive">Delete</Button>
  </AlertDialogTrigger>
  <AlertDialogContent>
    <AlertDialogHeader>
      <AlertDialogTitle>Are you sure?</AlertDialogTitle>
      <AlertDialogDescription>
        This action cannot be undone.
      </AlertDialogDescription>
    </AlertDialogHeader>
    <AlertDialogFooter>
      <AlertDialogCancel>Cancel</AlertDialogCancel>
      <AlertDialogAction onClick={handleDelete}>
        Delete
      </AlertDialogAction>
    </AlertDialogFooter>
  </AlertDialogContent>
</AlertDialog>
```

### Dropdown Menu

**Import:**
```typescript
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
```

**Example:**
```typescript
<DropdownMenu>
  <DropdownMenuTrigger asChild>
    <Button variant="outline">Open Menu</Button>
  </DropdownMenuTrigger>
  <DropdownMenuContent>
    <DropdownMenuLabel>My Account</DropdownMenuLabel>
    <DropdownMenuSeparator />
    <DropdownMenuItem>Profile</DropdownMenuItem>
    <DropdownMenuItem>Settings</DropdownMenuItem>
    <DropdownMenuItem onClick={handleLogout}>
      Logout
    </DropdownMenuItem>
  </DropdownMenuContent>
</DropdownMenu>
```

### Sheet (Side Panel)

**Import:**
```typescript
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet";
```

**Example:**
```typescript
<Sheet>
  <SheetTrigger asChild>
    <Button variant="outline">Open</Button>
  </SheetTrigger>
  <SheetContent side="right">
    <SheetHeader>
      <SheetTitle>Edit profile</SheetTitle>
      <SheetDescription>
        Make changes to your profile here.
      </SheetDescription>
    </SheetHeader>
    {/* Form or content */}
  </SheetContent>
</Sheet>
```

**Sides:** `left`, `right`, `top`, `bottom`

### Toast

**Import:**
```typescript
import { useToast } from "@/hooks/use-toast";
import { Toaster } from "@/components/ui/toaster";
```

**Setup (in App.tsx or layout):**
```typescript
function App() {
  return (
    <>
      {/* Your app */}
      <Toaster />
    </>
  );
}
```

**Usage:**
```typescript
function MyComponent() {
  const { toast } = useToast();

  const handleClick = () => {
    toast({
      title: "Success",
      description: "Your changes have been saved.",
    });
  };

  return <Button onClick={handleClick}>Save</Button>;
}
```

**Variants:**
```typescript
// Success
toast({
  title: "Success",
  description: "Operation completed.",
});

// Error
toast({
  title: "Error",
  description: "Something went wrong.",
  variant: "destructive",
});

// With action
toast({
  title: "Undo available",
  description: "Item deleted.",
  action: <ToastAction altText="Undo" onClick={handleUndo}>Undo</ToastAction>,
});
```

### Label

**Import:**
```typescript
import { Label } from "@/components/ui/label";
```

**Example:**
```typescript
<div>
  <Label htmlFor="email">Email</Label>
  <Input id="email" type="email" />
</div>
```

### Separator

**Import:**
```typescript
import { Separator } from "@/components/ui/separator";
```

**Example:**
```typescript
<div>
  <h2>Section 1</h2>
  <Separator className="my-4" />
  <h2>Section 2</h2>
</div>
```

**Orientation:**
```typescript
<Separator orientation="horizontal" />  {/* Default */}
<Separator orientation="vertical" className="h-20" />
```

## Custom Components

App-specific components in `src/components/`.

### Layout

**File:** `src/components/layout.tsx`

Main application layout shell.

**Props:**
```typescript
type LayoutProps = Readonly<PropsWithChildren>;
```

**Usage:**
```typescript
<Layout>
  <YourContent />
</Layout>
```

**Structure:**
- Renders `<NavHeader />` at top
- Wraps children in responsive container
- Applies consistent padding and styling

### NavHeader

**File:** `src/components/NavHeader.tsx`

Application navigation header with responsive design.

**Features:**
- Desktop horizontal nav bar
- Mobile hamburger menu (Sheet)
- Login/Logout button
- Active route highlighting
- Auto-hides nav items when not authenticated

**Navigation items** (`NavHeader.tsx:9-15`):
```typescript
const navItems = [
  { href: "/", label: "Home" },
  { href: "/plans", label: "Plans" },
  { href: "/coupons", label: "Coupons" },
  { href: "/clients", label: "Clients" },
  { href: "/subscribers", label: "Subscribers" },
];
```

**To add a nav item:**
```typescript
const navItems = [
  // ... existing items
  { href: "/my-page", label: "My Page" },
];
```

**Logout flow** (`NavHeader.tsx:43-50`):
1. Calls `/auth/logout` endpoint
2. Updates auth context via `setUnauthorized()`
3. Navigates to `/login`

### Snackbar

**File:** `src/components/Snackbar.tsx`

Toast notification system (custom implementation, alternative to shadcn Toast).

**Provider setup:**
```typescript
// In App.tsx
<SnackbarProvider>
  <YourApp />
</SnackbarProvider>
```

**Usage:**
```typescript
import { useSnackbar } from "@/components/Snackbar";

function MyComponent() {
  const { showSnackbar } = useSnackbar();

  const handleSuccess = () => {
    showSnackbar("success", "Changes saved!", 3000);
  };

  const handleError = () => {
    showSnackbar("error", "Failed to save changes", 5000);
  };

  const handleWarning = () => {
    showSnackbar("warning", "Please review your input");
  };

  return <Button onClick={handleSuccess}>Save</Button>;
}
```

**API:**
```typescript
showSnackbar(
  variant: "success" | "error" | "warning",
  message: string,
  timeout?: number  // Default: 3000ms
): void
```

**Styling:**
- Positioned bottom-right on desktop, bottom-center on mobile
- Auto-dismisses after timeout
- Manual dismiss via X button
- Dark mode support
- Icon indicators (CheckCircle, AlertCircle, AlertTriangle)

### ErrorDialog

**File:** `src/components/ErrorDialog.tsx`

Modal dialog for displaying errors.

**Props:**
```typescript
type ErrorDialogProps = Readonly<{
  title: string;
  error: Error;
  startOpen: boolean;
  onClose: () => void;
}>;
```

**Usage:**
```typescript
import ErrorDialog from "@/components/ErrorDialog";

function MyComponent() {
  const [error, setError] = useState<Error | null>(null);

  return (
    <>
      {error && (
        <ErrorDialog
          title="Operation Failed"
          error={error}
          startOpen={true}
          onClose={() => setError(null)}
        />
      )}
    </>
  );
}
```

**Used by:**
- `RelayEnvironmentProviderWrapper` for GraphQL errors
- Error boundaries
- Manual error displays

### BasicDialog

**File:** `src/components/BasicDialog.tsx`

General-purpose dialog component (needs verification - not shown in earlier reads).

### InputWithLabel

**File:** `src/components/InputWithLabel.tsx`

Convenience component combining Label + Input.

### PhoneNumberInput

**File:** `src/components/PhoneNumberInput.tsx`

Specialized input for phone numbers with formatting.

**Features:**
- Auto-formatting (e.g., (555) 123-4567)
- Uses `@react-input/mask`
- Integrates with React Hook Form

### TextInput

**File:** `src/components/TextInput.tsx`

Enhanced text input component.

### ModeToggle

**File:** `src/components/mode-toggle.tsx`

Dark/light mode toggle button.

**Usage:**
```typescript
import { ModeToggle } from "@/components/mode-toggle";

<ModeToggle />
```

Displays a dropdown menu with:
- Light theme
- Dark theme
- System theme (auto)

**Theme persistence:**
Managed by `ThemeProvider` using `localStorage` key: `vite-ui-theme`

### ThemeProvider

**File:** `src/components/theme-provider.tsx`

Context provider for theme management.

**Props:**
```typescript
type ThemeProviderProps = {
  children: React.ReactNode;
  defaultTheme?: "dark" | "light" | "system";
  storageKey?: string;
}
```

**Setup (main.tsx):**
```typescript
<ThemeProvider defaultTheme="dark" storageKey="vite-ui-theme">
  <App />
</ThemeProvider>
```

**Hook:**
```typescript
import { useTheme } from "@/components/theme-provider";

function MyComponent() {
  const { theme, setTheme } = useTheme();

  return (
    <Button onClick={() => setTheme("dark")}>
      Use Dark Theme
    </Button>
  );
}
```

## Utility Functions

### cn (Class Names)

**File:** `src/lib/utils.ts`

Utility for merging Tailwind classes.

**Import:**
```typescript
import { cn } from "@/lib/utils";
```

**Usage:**
```typescript
<div className={cn(
  "base-classes",
  isActive && "active-classes",
  isDisabled && "disabled-classes",
  className  // Props override
)}>
```

**How it works:**
- Uses `clsx` for conditional classes
- Uses `tailwind-merge` to resolve conflicts

**Example:**
```typescript
cn("px-4 py-2", "px-6")  // Result: "px-6 py-2"
// tailwind-merge removes conflicting px-4
```

## Adding New shadcn Components

To add additional shadcn/ui components:

```bash
npx shadcn@latest add <component-name>
```

**Examples:**
```bash
npx shadcn@latest add accordion
npx shadcn@latest add tabs
npx shadcn@latest add dialog
npx shadcn@latest add popover
```

This copies the component source to `src/components/ui/`.

**Full list:** https://ui.shadcn.com/docs/components

## Component Best Practices

### Composition Over Props

Prefer composing primitive components:

```typescript
// Good
<Card>
  <CardHeader>
    <CardTitle>{title}</CardTitle>
  </CardHeader>
  <CardContent>{content}</CardContent>
</Card>

// Avoid
<Card title={title} content={content} />
```

### Type Safety

Always type component props:

```typescript
type MyComponentProps = Readonly<{
  title: string;
  onSubmit: (data: FormData) => void;
  className?: string;
}>;

export function MyComponent({ title, onSubmit, className }: MyComponentProps) {
  // ...
}
```

### Dark Mode Support

Use semantic color tokens:

```typescript
// Good - adapts to theme
<div className="bg-background text-foreground border-border">

// Avoid - hardcoded colors
<div className="bg-white text-black border-gray-300">
```

**Color tokens:**
- `bg-background` / `text-foreground` - Main bg/text
- `bg-card` / `text-card-foreground` - Card surfaces
- `bg-primary` / `text-primary-foreground` - Primary brand
- `bg-secondary` / `text-secondary-foreground` - Secondary
- `bg-muted` / `text-muted-foreground` - Muted/disabled
- `border` - Border color
- `input` - Input border
- `ring` - Focus ring

### Accessibility

shadcn/ui components are accessible by default (Radix UI primitives). Maintain accessibility:

- Use semantic HTML
- Provide `aria-label` for icon buttons
- Use `Label` with form inputs
- Ensure keyboard navigation works
- Test with screen readers

**Example:**
```typescript
<Button variant="ghost" size="icon" aria-label="Close menu">
  <X className="h-4 w-4" />
</Button>
```

## Style Customization

### Component-Level

Override via `className`:

```typescript
<Button className="bg-purple-500 hover:bg-purple-600">
  Custom Color
</Button>
```

### Global Theme

Edit `src/index.css` CSS variables:

```css
@layer base {
  :root {
    --background: 0 0% 100%;
    --foreground: 222.2 84% 4.9%;
    --primary: 222.2 47.4% 11.2%;
    /* ... */
  }

  .dark {
    --background: 222.2 84% 4.9%;
    --foreground: 210 40% 98%;
    /* ... */
  }
}
```

**Theme generator:** https://ui.shadcn.com/themes

## Next Steps

- [Development Guide](DEVELOPMENT.md) - Development workflows
- [Screens](SCREENS.md) - Screen-specific documentation
- [Architecture](ARCHITECTURE.md) - System architecture
