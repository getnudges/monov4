"use client";

import { useState } from "react";
import {
  Bell,
  Calendar,
  CreditCard,
  Download,
  MoreHorizontal,
  Search,
  Settings,
  Users,
} from "lucide-react";
import { DateTime } from "luxon";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Progress } from "@/components/ui/progress";

interface ClientDashboardProps {
  client: any; // In a real app, you would define a proper type for this
}

export function ClientDashboard({ client }: ClientDashboardProps) {
  const [searchQuery, setSearchQuery] = useState("");

  // Format the joined date
  const formattedJoinedDate = new Date(client.joinedDate).toLocaleDateString(
    "en-US",
    {
      year: "numeric",
      month: "long",
      day: "numeric",
    }
  );

  // Calculate subscription remaining time
  const subscriptionEndDate = new Date(client.subscription.endDate);
  const totalDays =
    (subscriptionEndDate.getTime() -
      new Date(client.subscription.startDate).getTime()) /
    (1000 * 3600 * 24);
  const daysRemaining =
    (subscriptionEndDate.getTime() - new Date().getTime()) / (1000 * 3600 * 24);
  const percentRemaining = Math.max(
    0,
    Math.min(100, (daysRemaining / totalDays) * 100)
  );

  return (
    <div className="flex min-h-screen w-full flex-col">
      <header className="sticky top-0 z-10 flex h-16 items-center gap-4 border-b bg-background px-4 md:px-6">
        <div className="flex items-center gap-2 font-semibold">
          <Users className="h-6 w-6" />
          <span>Client Dashboard</span>
        </div>
        <div className="ml-auto flex items-center gap-4">
          <form className="relative">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              type="search"
              placeholder="Search..."
              className="w-[200px] pl-8 md:w-[300px] lg:w-[400px]"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
          </form>
          <Button variant="outline" size="icon">
            <Bell className="h-4 w-4" />
            <span className="sr-only">Notifications</span>
          </Button>
          <Button variant="outline" size="icon">
            <Settings className="h-4 w-4" />
            <span className="sr-only">Settings</span>
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm" className="gap-2">
                <Avatar className="h-6 w-6">
                  <AvatarImage src="/placeholder.svg" alt="Avatar" />
                  <AvatarFallback>AC</AvatarFallback>
                </Avatar>
                <span className="hidden md:inline-flex">Admin</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>My Account</DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem>Profile</DropdownMenuItem>
              <DropdownMenuItem>Settings</DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem>Logout</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </header>
      <main className="flex-1 p-4 md:p-6">
        <div className="flex flex-col gap-4 md:gap-8">
          <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
            <div>
              <h1 className="text-3xl font-bold tracking-tight">
                {client.name}
              </h1>
              <p className="text-muted-foreground">
                Client since {formattedJoinedDate} • {client.locale}
              </p>
            </div>
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" className="gap-1">
                <Download className="h-4 w-4" />
                Export
              </Button>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="sm" className="gap-1">
                    <MoreHorizontal className="h-4 w-4" />
                    Actions
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem>Edit Client</DropdownMenuItem>
                  <DropdownMenuItem>Manage Subscription</DropdownMenuItem>
                  <DropdownMenuItem>View Activity Log</DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>

          <Tabs defaultValue="overview" className="space-y-4">
            <TabsList>
              <TabsTrigger value="overview">Overview</TabsTrigger>
              <TabsTrigger value="subscribers">Subscribers</TabsTrigger>
              <TabsTrigger value="subscription">Subscription</TabsTrigger>
              <TabsTrigger value="announcements">Announcements</TabsTrigger>
            </TabsList>

            <TabsContent value="overview" className="space-y-4">
              <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
                <Card>
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">
                      Total Subscribers
                    </CardTitle>
                    <Users className="h-4 w-4 text-muted-foreground" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">
                      {client.subscriberCount.toLocaleString()}
                    </div>
                    <p className="text-xs text-muted-foreground">
                      +12% from last month
                    </p>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">
                      Subscription Status
                    </CardTitle>
                    <CreditCard className="h-4 w-4 text-muted-foreground" />
                  </CardHeader>
                  <CardContent>
                    <div className="flex items-center gap-2">
                      <div className="text-2xl font-bold capitalize">
                        {client.subscription.status}
                      </div>
                      <Badge
                        variant={
                          client.subscription.status === "active"
                            ? "default"
                            : "destructive"
                        }
                      >
                        {client.subscription.status}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      {client.subscription.priceTier?.name} Plan
                    </p>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">
                      Subscription Ends
                    </CardTitle>
                    <Calendar className="h-4 w-4 text-muted-foreground" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">
                      {DateTime.fromJSDate(
                        new Date(client.subscription.endDate)
                      ).toFormat("MMM d, yyyy")}
                    </div>
                    <div className="mt-2 space-y-1">
                      <p className="text-xs text-muted-foreground">
                        {Math.ceil(daysRemaining)} days remaining
                      </p>
                      <Progress value={percentRemaining} className="h-2" />
                    </div>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">
                      Client ID
                    </CardTitle>
                    <CreditCard className="h-4 w-4 text-muted-foreground" />
                  </CardHeader>
                  <CardContent>
                    <div className="font-mono text-sm font-medium">
                      {client.id}
                    </div>
                    <p className="text-xs text-muted-foreground mt-1">
                      Customer ID: {client.customerId}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      Slug: {client.slug}
                    </p>
                  </CardContent>
                </Card>
              </div>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                <Card className="md:col-span-2">
                  <CardHeader>
                    <CardTitle>Recent Subscribers</CardTitle>
                    <CardDescription>
                      Showing the 5 most recent subscribers
                    </CardDescription>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-4">
                      {client.recentSubscribers.map((subscriber: any) => (
                        <div
                          key={subscriber.id}
                          className="flex items-center gap-4"
                        >
                          <Avatar>
                            <AvatarFallback>
                              {subscriber.name
                                .split(" ")
                                .map((n: string) => n[0])
                                .join("")}
                            </AvatarFallback>
                          </Avatar>
                          <div className="flex-1 space-y-1">
                            <p className="text-sm font-medium leading-none">
                              {subscriber.name}
                            </p>
                            <p className="text-sm text-muted-foreground">
                              {subscriber.email}
                            </p>
                          </div>
                          <div className="text-sm text-muted-foreground">
                            {DateTime.fromJSDate(
                              new Date(subscriber.joinedDate)
                            )
                              .diffNow()
                              .toFormat("d 'days ago'")}
                          </div>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                  <CardFooter>
                    <Button variant="outline" className="w-full">
                      View All Subscribers
                    </Button>
                  </CardFooter>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle>Contact Information</CardTitle>
                    <CardDescription>Client contact details</CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="space-y-1">
                      <p className="text-sm font-medium leading-none">
                        Phone Number
                      </p>
                      <p className="text-sm text-muted-foreground">
                        {client.phoneNumber}
                      </p>
                    </div>
                    <div className="space-y-1">
                      <p className="text-sm font-medium leading-none">Locale</p>
                      <p className="text-sm text-muted-foreground">
                        {client.locale}
                      </p>
                    </div>
                    <div className="space-y-1">
                      <p className="text-sm font-medium leading-none">
                        Customer ID
                      </p>
                      <p className="text-sm font-mono text-muted-foreground">
                        {client.customerId}
                      </p>
                    </div>
                  </CardContent>
                  <CardFooter>
                    <Button variant="outline" className="w-full">
                      Edit Contact Info
                    </Button>
                  </CardFooter>
                </Card>
              </div>
            </TabsContent>

            <TabsContent value="subscribers" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle>Subscribers</CardTitle>
                  <CardDescription>
                    Manage and view all subscribers for {client.name}
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <p className="text-muted-foreground">
                    This client has {client.subscriberCount.toLocaleString()}{" "}
                    total subscribers. Use the search and filter options to find
                    specific subscribers.
                  </p>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="subscription" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle>Subscription Details</CardTitle>
                  <CardDescription>
                    Current subscription information and history
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                    <div className="space-y-1">
                      <p className="text-sm font-medium leading-none">Plan</p>
                      <p className="text-lg font-bold">
                        {client.subscription.priceTier?.name}
                      </p>
                    </div>
                    <div className="space-y-1">
                      <p className="text-sm font-medium leading-none">Status</p>
                      <div className="flex items-center gap-2">
                        <p className="text-lg font-bold capitalize">
                          {client.subscription.status}
                        </p>
                        <Badge
                          variant={
                            client.subscription.status === "active"
                              ? "default"
                              : "destructive"
                          }
                        >
                          {client.subscription.status}
                        </Badge>
                      </div>
                    </div>
                    <div className="space-y-1">
                      <p className="text-sm font-medium leading-none">
                        Start Date
                      </p>
                      <p className="text-lg">
                        {DateTime.fromJSDate(
                          new Date(client.subscription.startDate)
                        ).toFormat("MMMM d, yyyy")}
                      </p>
                    </div>
                    <div className="space-y-1">
                      <p className="text-sm font-medium leading-none">
                        End Date
                      </p>
                      <p className="text-lg">
                        {DateTime.fromJSDate(
                          new Date(client.subscription.endDate)
                        ).toFormat("MMMM d, yyyy")}
                      </p>
                    </div>
                    <div className="space-y-1">
                      <p className="text-sm font-medium leading-none">
                        Subscription ID
                      </p>
                      <p className="text-sm font-mono">
                        {client.subscription.id}
                      </p>
                    </div>
                    <div className="space-y-1">
                      <p className="text-sm font-medium leading-none">
                        Time Remaining
                      </p>
                      <div className="space-y-1">
                        <p className="text-lg">
                          {Math.ceil(daysRemaining)} days
                        </p>
                        <Progress value={percentRemaining} className="h-2" />
                      </div>
                    </div>
                  </div>
                </CardContent>
                <CardFooter className="flex flex-col gap-2 sm:flex-row">
                  <Button className="w-full sm:w-auto">
                    Renew Subscription
                  </Button>
                  <Button variant="outline" className="w-full sm:w-auto">
                    Change Plan
                  </Button>
                </CardFooter>
              </Card>
            </TabsContent>

            <TabsContent value="announcements" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle>Announcements</CardTitle>
                  <CardDescription>
                    Recent announcements for {client.name}
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-6">
                    {client.announcements.map((announcement: any) => (
                      <div key={announcement.id} className="space-y-2">
                        <div className="flex items-center justify-between">
                          <h3 className="font-semibold">
                            {announcement.title}
                          </h3>
                          <p className="text-sm text-muted-foreground">
                            {DateTime.fromJSDate(
                              new Date(announcement.date)
                            ).toFormat("MMM d, yyyy")}
                          </p>
                        </div>
                        <p className="text-sm text-muted-foreground">
                          {announcement.content}
                        </p>
                      </div>
                    ))}
                  </div>
                </CardContent>
                <CardFooter>
                  <Button variant="outline" className="w-full">
                    Create New Announcement
                  </Button>
                </CardFooter>
              </Card>
            </TabsContent>
          </Tabs>
        </div>
      </main>
    </div>
  );
}
