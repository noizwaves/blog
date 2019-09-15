---
title:  Fixing Slow Spring Boot Startup
date:   2017-09-02 17:56:00 -0600
---

### TL;DR

If you are running MacOS, add hostname (output of `hostname`) to the IPv4 & IPv6 loopback entries in `/etc/hosts` to cut 10 seconds from Spring Boot startup time.

### The Problem

Spring Boot has ALWAYS started slow for me.
Almost embarrassingly slow.
How slow?
Start with an empty Spring Boot Web application with Spring Initializer.

![Spring Initializer](/assets/slow-spring-boot-startup/spring-initializer.png)

Now launch the server using `./gradlew bootRun` and take a look at that launch time.

![Slow bootRun](/assets/slow-spring-boot-startup/bootRun-slow.png)

**12.184 seconds!!**
To load nothing but framework.
Yuk!

It turns out as part of Spring Boot startup it makes several calls to `InetAddress.getLocalHost().getHostName()`, and [this is known to be slow](https://github.com/spring-projects/spring-boot/issues/7087).

[Antonio Troina](https://thoeni.io) released an awesome [post](https://thoeni.io/post/macos-sierra-java/) and [code sample](https://github.com/thoeni/inetTester) that highlights the issue.
Running inetTester produced this output:

![Slow inetTester](/assets/slow-spring-boot-startup/inetTester-slow.png)

The `InetAddress.getLocalHost().getHostName()` call took over 5 seconds.

### The Solution

A solution exists, and that is to explicitly add your hostname into the IPv4 and IPv6 loopback interface entries in the hosts file.

Before, My standard-ish `/etc/hosts` file used to look like this:

![Slow etc hosts](/assets/slow-spring-boot-startup/etc-hosts-slow.png)

Here is what my `/etc/hosts` looks like now:

![Fast etc hosts](/assets/slow-spring-boot-startup/etc-hosts-fast.png)

Now with this change (and no reboot or no terminal restarts), inetTester outputs this:

![Fast inetTester](/assets/slow-spring-boot-startup/inetTester-fast.png)

**9 ms!**
Down from 5011 ms.

What effect does this have on Spring Boot's startup time?
Take a look for yourself.

![Fast bootRun](/assets/slow-spring-boot-startup/bootRun-fast.png)


**2.264 seconds.**
Down from 12.184 seconds.