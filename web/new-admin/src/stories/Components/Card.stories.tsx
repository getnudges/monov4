import type { Meta, StoryObj } from "@storybook/react";
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  CardFooter,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const meta = {
  title: "Components/Card",
  component: Card,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
} satisfies Meta<typeof Card>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Basic: Story = {
  render: () => (
    <Card className="w-[350px]">
      <CardHeader>
        <CardTitle>Card Title</CardTitle>
        <CardDescription>Card description goes here</CardDescription>
      </CardHeader>
      <CardContent>
        <p>Card content. Lorem ipsum dolor sit amet, consectetur adipiscing elit.</p>
      </CardContent>
      <CardFooter>
        <Button>Action</Button>
      </CardFooter>
    </Card>
  ),
};

export const WithForm: Story = {
  render: () => (
    <Card className="w-[350px]">
      <CardHeader>
        <CardTitle>Create Account</CardTitle>
        <CardDescription>Enter your details to create a new account</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="name">Name</Label>
          <Input id="name" placeholder="Enter your name" />
        </div>
        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <Input id="email" type="email" placeholder="Enter your email" />
        </div>
      </CardContent>
      <CardFooter>
        <Button className="w-full">Create Account</Button>
      </CardFooter>
    </Card>
  ),
};

export const PlanCard: Story = {
  render: () => (
    <Card className="w-[350px]">
      <CardHeader>
        <CardTitle>Basic Plan</CardTitle>
        <CardDescription>Perfect for getting started</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="text-3xl font-bold">$9.99/mo</div>
        <ul className="mt-4 space-y-2 text-sm">
          <li>✓ 100 messages per month</li>
          <li>✓ Basic support</li>
          <li>✓ Email notifications</li>
        </ul>
      </CardContent>
      <CardFooter>
        <Button variant="outline" className="w-full">
          Choose Plan
        </Button>
      </CardFooter>
    </Card>
  ),
};

export const NoFooter: Story = {
  render: () => (
    <Card className="w-[350px]">
      <CardHeader>
        <CardTitle>Statistics</CardTitle>
        <CardDescription>Your account metrics</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="space-y-2">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Total Plans</span>
            <span className="font-semibold">12</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Active Subscribers</span>
            <span className="font-semibold">1,234</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Revenue</span>
            <span className="font-semibold">$12,345</span>
          </div>
        </div>
      </CardContent>
    </Card>
  ),
};
