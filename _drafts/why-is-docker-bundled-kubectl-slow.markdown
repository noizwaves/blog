---
layout: post
title:  "Why is Docker Desktop bundled kubectl slow?"
tags: docker kubectl performance debugging
date:   2021-05-22 15:16:00 -0600
---

# The TL;DR


# Backstory

For the last few months I've been deploying a new kubernetes based cloud development environment platform powered by [Loft](https://loft.sh/) and [DevSpace](https://devspace.sh/).
DevSpace is a developer centric CLI that makes typical developer workflows effortless with kubernetes.
Loft is an administrative enhancement to kubernetes that makes administering kubernetes based development environments effortless.
The premise of running development environments in the cloud is that you shift the workload from a resource constrained laptop (4-6 cores and 16gb of memory) to a horizontally-scalable kubernetes cluster that can add extra resources on demand.

While building this system, we end up running `kubectl` commands **very frequently**; my most common command is `kubectl get pods`.
Executing this command was lightning quick when we first started out - primarily because we started developing the system against local minikube clusters.

As we started building out a production ready cluster, we ended up adding more network hops in between our laptops and the cluster.
These are the things you typically have in any organization, like VPNs, traffic gateways, and load balancers.
Each one adds a little latency.

# The Problem

When the cluster was finished and accessible from a laptop, `kubectl get pods` felt slow.
And not just like enterprise distributed systems slow either, but unjustifiably slow.
Getting a list of pods shouldn't take 5 seconds!

One day curiosity got the better of me, and I decided to dig in.
This is a walkthrough of how I debugged the slowness.

> Initial theory: something to do with SSL in between a gateway and a load balancer

# 1. Establish a baseline measurement

The first step was to turn "unjustifiably slow" into a number.
The `time` command works great for this:
```
$ time kubectl get pods
NAME                                      READY   STATUS    RESTARTS   AGE
db-676c97cf7d-hdfvp                       1/1     Running   0          6m30s
web-bd5d48686-lpbtj                       2/2     Running   0          6m30s
kubectl get pods  0.25s user 0.12s system 6% cpu 5.640 total
```

The baseline is **5640ms**.

**Note:** all command outputs have been modified for clean viewing on the web.
Important details have been left unmodified.

# 2. More detail

The verbosity of `kubectl` can be controlled with the `-v` flag.
I've used this in the past when debugging previous issues.
Maybe this could be used to get some more insight into what is going on during the 5.64 seconds.
Setting `-v6` gave the perfect amount of information (some output is truncated for optimal blogging experience):
```
$ kubectl get pods -v6
Config loaded from file:  /Users/adam.neumann/.kube/config
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1 200 OK in 2108 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1 200 OK in 2121 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1 200 OK in 1627 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1 200 OK in 1636 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1 200 OK in 122 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1 200 OK in 126 milliseconds
GET https://loft/kubernetes/api/v1/namespaces/adam-neumann/pods?limit=500 200 OK in 1646 milliseconds
NAME                                      READY   STATUS    RESTARTS   AGE
db-676c97cf7d-hdfvp                       1/1     Running   0          19m
web-bd5d48686-lpbtj                       2/2     Running   0          19m
```

Look at all of those slow requests! **1646ms** to get the list of pods!
And **2108ms** to get the custom metrics.
Those slow requests point to some network shenanigans going on.
I remember in the past, running `kubectl` commands from a bastion host in the cloud was fast, so suspicion was now on the VPN.

> Updated theory: VPN is somehow slowing down the requests.

**Note**: I actually missed the two fast requests (122ms and 126ms) during debugging.
These two (and only these two) requests are always fast, and actually disprove the theory about network shenanigans.

# 3. Eliminate the VPN

I wanted to eliminate the VPN as the source of the slowness.
To do this, I had to run `kubectl` from inside the network, but still transit the gateways and load balancers.

After logging into the bastion host and setting up DevSpace, running `kubectl` was straight forward:
```
$ kubectl get pods -v6
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 146 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 145 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 7 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 7 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 7 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 9 milliseconds
GET https://loft/kubernetes/api/v1/namespaces/adam-neumann/pods?limit=500 200 OK in 9 milliseconds
NAME                                      READY   STATUS    RESTARTS   AGE
db-676c97cf7d-hdfvp                       1/1     Running   0          38m
web-bd5d48686-lpbtj                       2/2     Running   0          38m
```

Ultimate success!!
Eliminating the VPN reduced all request times by at least one order of magnitude, and up to two orders of magnitude for others.
This was the smoking gun I'd been looking for.

> Updated theory: VPN was somehow interfering with the HTTP requests, probably SSL handshaking because that involves a lot of round trips, probably connection related.

# 4. Examining request behaviour with curl

Now I was curious what was going on at a request level.
The `curl` command can be used to make individual HTTP requests with a variety of options, and this would be an easy way to look for any suspicious SSL/connection information
I picked the `GET https://loft/kubernetes/api/v1/namespaces/adam-neumann/pods?limit=500` as the request to poke at.

The first step to curling this endpoint is getting a valid auth token for kubernetes.
It turns out this is pretty straigth forward to do, especially when using Loft.
Loft utilizes a helper command to generate tokens automatically when using `kubectl`, and this can be called manually:
```
$ ~/.devspace/plugins/LONGALPHANUM/binary token --silent --config ~/.loft/config.json
{"kind":"ExecCredential",...,"status":{"token":"LONGALPHANUMERICTOKENVALUE"}}
```

With this token, we can use curl to make the request, and [have it output relevant timing information](https://help.heroku.com/NY64S5NL/how-do-i-debug-latency-issues-using-curl).
The output values are in seconds and represent the time taken to reach that event during the request lifecycle.

Firstly, running curl on the bastion host:
```
$ curl \
  -w "dns_resolution: %{time_namelookup}, tcp_established: %{time_connect}, ssl_handshake_done: %{time_appconnect}, TTFB: %{time_starttransfer}, Total: %{time_total}\n" \
  -o /dev/null \
  -H "Authorization: Bearer LONGALPHANUMERICTOKENVALUE" \
  -s "https://loft/kubernetes/api/v1/namespaces/adam-neumann/pods?limit=500"
dns_resolution: 0.004, tcp_established: 0.007, ssl_handshake_done: 0.018, TTFB: 0.026, Total: 0.026
```

And then running it locally:
```
$ curl \
  -w "dns_resolution: %{time_namelookup}, tcp_established: %{time_connect}, ssl_handshake_done: %{time_appconnect}, TTFB: %{time_starttransfer}, Total: %{time_total}\n" \
  -o /dev/null \
  -H "Authorization: Bearer LONGALPHANUMERICTOKENVALUE" \
  -s "https://loft/kubernetes/api/v1/namespaces/adam-neumann/pods?limit=500"
dns_resolution: 0.002292, tcp_established: 0.134979, ssl_handshake_done: 0.393691, TTFB: 0.521001, Total: 0.521263
```

Digging into the results:
- on the bastion host, SSL handshake took 9ms (18ms - 7ms), and represents about a third of the total request time (26ms)
- locally, The SSL handshake took 259ms (394ms - 135ms), and represents about half of the total request time (**521ms**)

Hold up a minute, 521ms to make the GET request using `curl`... that same request took 1646ms with `kubectl`!
1 second is a huge difference!
This is probably due to a difference between how `curl` and `kubectl` handle connections.
`curl` is probably keeping them open, and `kubectl` is closing them immediately.

> Updated theory: `kubectl` is closing connections after each request, adding overhead that makes the requests slow.

# 5. Forcing curl to close connections

To prove that the extra second from `kubectl` was from connections closing, we could force `curl` to close connections too.
It is straight forward to do this by setting the `Connection` header.
Running the modified command locally:
```
$ curl \
  <original arguments ommitted>
  -H "Connection: close"
dns_resolution: 0.002739, tcp_established: 0.122931, ssl_handshake_done: 0.390603, TTFB: 0.523905, Total: 0.524021
```

Closing connections had no effect on the timings.
Darn.
Back to the drawing board...

> Updated theory: there is something about `kubectl` that is making the requests slow.

# 6. kubectl versions

Not being sure where to start poking around with `kubectl`, I decided to start by checking the version.
I have no memory of installing `kubectl`, so at best the version is some arbitrary version.
Finding the version of `kubectl` is straight forward:
```
$ kubectl version
Client Version: version.Info{Major:"1", Minor:"19", GitVersion:"v1.19.7", ...}
Server Version: version.Info{Major:"1", Minor:"17+", GitVersion:"v1.17.12-eks-7684af", ...}
```

The `kubectl version` command is helpful in that it prints both the client (i.e. `kubectl`) version as well as the server (i.e. the cluster) version.
My client version is 2 minor versions ahead of the cluster!

> Updated theory: client/server version mismatch is interfering with the requests.

# 7. Downgrading kubectl to match cluster

This version mismatch theory is easy to test as `kubectl` binaries can be easily downloaded from the internet:
```
$ curl -s -L "https://dl.k8s.io/release/v1.17.12/bin/darwin/amd64/kubectl" -o kubectl-1.17.12
$ chmod +x kubectl-1.17.12
```

Then confirming the versions match:
```
$ ./kubectl-1.17.12 version
Client Version: version.Info{Major:"1", Minor:"17", GitVersion:"v1.17.12", ...}
Server Version: version.Info{Major:"1", Minor:"17+", GitVersion:"v1.17.12-eks-7684af", ...}
```

Now to test with a `get pods` call:
```
$ ./kubectl-1.17.12 get pods -v6
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 901 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 907 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 126 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 129 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 129 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 130 milliseconds
GET https://loft/kubernetes/api/v1/namespaces/adam-neumann/pods?limit=500 200 OK in 134 milliseconds
NAME                                      READY   STATUS    RESTARTS   AGE
db-676c97cf7d-hdfvp                       1/1     Running   0          117m
web-bd5d48686-lpbtj                       2/2     Running   0          117m
```

WOW!
**134ms** for the `GET https://loft/kubernetes/api/v1/namespaces/adam-neumann/pods?limit=500` request.
That is down from **1646ms** with version 1.19.7.
Version mismatch is a real thing.

> Updated theory: client/server version mismatch causes slow requests.

This kind of makes sense.
Versions exist for a reason, and if they're different, there could be some incompatibility.
After fixing the version locally, the latency went away.
The bastion host must have had matching versions this whole time!

# 8. Checking bastion host versions

Checking the `kubectl` version on the bastion host is straight forward:
```
$ kubectl version
Client Version: version.Info{Major:"1", Minor:"15", GitVersion:"v1.15.1", ...}
Server Version: version.Info{Major:"1", Minor:"17+", GitVersion:"v1.17.12-eks-7684af", ...}
```

Huh?
Version mismatch on the bastion host - interesting!

Maybe I just got unlucky with my particular version difference.

> Updated theory: a version mismatch across a server of 1.17.12 and client of 1.19.7 causes slow requests.

# 9. Finding which version introduces the slowness

Now that we know that client version 1.17.12 is fast and 1.19.7 is slow, we could do a binary search style search to find which version introduces the slow requests!

I started with the latest 1.18 (1.18.19 at time of writing) client:
```
$ curl -s -L "https://dl.k8s.io/release/v1.18.19/bin/darwin/amd64/kubectl" -o kubectl-1.18.19
$ chmod +x kubectl-1.18.19
$ ./kubectl-1.18.19 get pods -v6
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 1057 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 1058 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 694 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 694 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 119 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 123 milliseconds
GET https://loft/kubernetes/api/v1/namespaces/adam-neumann/pods?limit=500 200 OK in 633 milliseconds
NAME                                      READY   STATUS    RESTARTS   AGE
db-676c97cf7d-hdfvp                       1/1     Running   0          149m
web-bd5d48686-lpbtj                       2/2     Running   0          149m
```

**1.18.19 is fast**

Next, a version in between 1.18.19 and 1.19.7, like 1.19.3.
```
$ curl -s -L "https://dl.k8s.io/release/v1.19.3/bin/darwin/amd64/kubectl" -o kubectl-1.19.3
$ chmod +x kubectl-1.19.3
$ ./kubectl-1.19.3 get pods -v6
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 842 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 849 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 123 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 127 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 135 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 137 milliseconds
GET https://loft/kubernetes/api/v1/namespaces/adam-neumann/pods?limit=500 200 OK in 129 milliseconds
NAME                                      READY   STATUS    RESTARTS   AGE
db-676c97cf7d-hdfvp                       1/1     Running   0          155m
web-bd5d48686-lpbtj                       2/2     Running   0          155m
```

**1.19.3 is fast**

Next, a version in between 1.19.3 and 1.19.7, like 1.19.5.
```
$ curl -s -L "https://dl.k8s.io/release/v1.19.5/bin/darwin/amd64/kubectl" -o kubectl-1.19.5
$ chmod +x kubectl-1.19.5
$ ./kubectl-1.19.5 get pods -v6
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 814 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 817 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 625 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 627 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 119 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 124 milliseconds
GET https://loft/kubernetes/api/v1/namespaces/adam-neumann/pods?limit=500 200 OK in 639 milliseconds
NAME                                      READY   STATUS    RESTARTS   AGE
db-676c97cf7d-hdfvp                       1/1     Running   0          160m
web-bd5d48686-lpbtj                       2/2     Running   0          160m
```

**1.19.5 is fast**

Next, a version in between 1.19.5 and 1.19.7, the only one being 1.19.6!
```
$ curl -s -L "https://dl.k8s.io/release/v1.19.6/bin/darwin/amd64/kubectl" -o kubectl-1.19.6
$ chmod +x kubectl-1.19.6
$ ./kubectl-1.19.6 get pods -v6
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 840 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 852 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 621 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 664 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 121 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 121 milliseconds
GET https://loft/kubernetes/api/v1/namespaces/adam-neumann/pods?limit=500 200 OK in 628 milliseconds
NAME                                      READY   STATUS    RESTARTS   AGE
db-676c97cf7d-hdfvp                       1/1     Running   0          166m
web-bd5d48686-lpbtj                       2/2     Running   0          166m
```

**1.19.6 is fast**

So 1.19.6 is fast, and 1.19.7 is slow - that must mean the issue was introduced in 1.19.7.

> Updated theory: kubectl version 1.19.7 introduces the slow requests 

# 10. Confirming issue in 1.19.7

This will be straight forward to confirm with the established method:
```
$ curl -s -L "https://dl.k8s.io/release/v1.19.7/bin/darwin/amd64/kubectl" -o kubectl-1.19.7
$ chmod +x kubectl-1.19.7
$ ./kubectl-1.19.7 get pods -v6
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 950 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 963 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 584 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 598 milliseconds
GET https://loft/kubernetes/apis/external.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 139 milliseconds
GET https://loft/kubernetes/apis/custom.metrics.k8s.io/v1beta1?timeout=32s 200 OK in 142 milliseconds
GET https://loft/kubernetes/api/v1/namespaces/adam-neumann/pods?limit=500 200 OK in 658 milliseconds
NAME                                      READY   STATUS    RESTARTS   AGE
db-676c97cf7d-tbr7x                       1/1     Running   0          28s
web-bd5d48686-xkm4l                       2/2     Running   0          28s
```

And **1.19.7 is... slow?!**
But how?
It's the same client version - or is it?

> Updated theory: something is wrong with the installed kubectl client version 1.19.7

# 11. Poking at kubectl and ./kubectl-1.19.7

They both binaries exhibit consistent performance characteristics:
```
$ seq 10 | xargs -I -- time ./kubectl-1.19.7 get pods >> /dev/null
        2.48 real         0.23 user         0.11 sys
        2.43 real         0.22 user         0.10 sys
        2.52 real         0.22 user         0.09 sys
        2.46 real         0.22 user         0.10 sys
        2.43 real         0.20 user         0.08 sys
        2.47 real         0.21 user         0.08 sys
        2.43 real         0.20 user         0.09 sys
        2.48 real         0.22 user         0.09 sys
        2.50 real         0.22 user         0.10 sys
        2.48 real         0.23 user         0.10 sys

$ seq 10 | xargs -I -- time kubectl get pods >> /dev/null
        5.62 real         0.25 user         0.12 sys
        6.04 real         0.22 user         0.09 sys
        6.72 real         0.24 user         0.10 sys
        5.56 real         0.24 user         0.11 sys
        5.64 real         0.26 user         0.13 sys
        5.66 real         0.23 user         0.11 sys
        5.60 real         0.24 user         0.11 sys
        5.57 real         0.23 user         0.11 sys
        5.78 real         0.22 user         0.16 sys
        5.81 real         0.23 user         0.12 sys
```

They both report exactly the same complete version information:
```
$ ./kubectl-1.19.7 version --client
Client Version: version.Info{Major:"1", Minor:"19", GitVersion:"v1.19.7", GitCommit:"1dd5338295409edcfff11505e7bb246f0d325d15", GitTreeState:"clean", BuildDate:"2021-01-13T13:23:52Z", GoVersion:"go1.15.5", Compiler:"gc", Platform:"darwin/amd64"}

$ kubectl version --client
Client Version: version.Info{Major:"1", Minor:"19", GitVersion:"v1.19.7", GitCommit:"1dd5338295409edcfff11505e7bb246f0d325d15", GitTreeState:"clean", BuildDate:"2021-01-13T13:23:52Z", GoVersion:"go1.15.5", Compiler:"gc", Platform:"darwin/amd64"}
```

Maybe the location of the binary is a factor?

> Updated theory: location of kubectl binary causes slow requests

# 12. Run the installed kubectl from a different directory

Locating the binary is straight forward:
```
$ which kubectl
/usr/local/bin/kubectl
```

Let's run it from the current directory (which is `~/workspace/my-app`):
```
$ cp /usr/local/bin/kubectl ./
$ seq 10 | xargs -I -- time ./kubectl get pods >> /dev/null
        2.72 real         0.21 user         0.09 sys
        2.67 real         0.23 user         0.11 sys
        2.60 real         0.24 user         0.12 sys
        2.80 real         0.23 user         0.11 sys
        2.62 real         0.22 user         0.10 sys
        2.97 real         0.22 user         0.10 sys
        2.68 real         0.24 user         0.11 sys
        2.78 real         0.24 user         0.11 sys
        2.73 real         0.24 user         0.11 sys
        2.62 real         0.24 user         0.09 sys
```

`~/workspace/my-app` is fast, and `/usr/local/bin/kubectl` is slow.

What about from other directories, like `~/` or `~/workspace`?
```
$ cp /usr/local/bin/kubectl ~/
$ seq 10 | xargs -I -- time ~/kubectl get pods >> /dev/null
        5.92 real         0.21 user         0.16 sys
        5.65 real         0.24 user         0.10 sys
        5.75 real         0.24 user         0.12 sys
        6.01 real         0.24 user         0.11 sys
        6.33 real         0.24 user         0.12 sys
        5.68 real         0.25 user         0.13 sys
        6.48 real         0.21 user         0.09 sys
        5.68 real         0.23 user         0.12 sys
        5.67 real         0.22 user         0.11 sys
        4.71 real         0.23 user         0.10 sys
```

`~/kubectl` is slow.

```
$ cp /usr/local/bin/kubectl ~/workspace
$ seq 10 | xargs -I -- time ~/workspace/kubectl get pods >> /dev/null
        2.91 real         0.22 user         0.10 sys
        2.72 real         0.26 user         0.13 sys
        2.68 real         0.21 user         0.09 sys
        2.57 real         0.22 user         0.10 sys
        2.72 real         0.23 user         0.10 sys
        2.73 real         0.24 user         0.12 sys
        2.66 real         0.23 user         0.08 sys
        2.66 real         0.24 user         0.10 sys
        2.74 real         0.24 user         0.10 sys
        2.65 real         0.23 user         0.09 sys
```

`~/workspace/kubectl` is fast.

To summarise these findings:
| Path | Speed |
|------|-------|
| /usr/local/bin/kubectl | Slow |
| ~/kubectl | Slow |
| ~/workspace/kubectl | Fast |
| ~/workspace/my-app/kubectl | Fast |

Seeing this pattern immediately made me suspicious that our security system.
We have had issues with conflicts in the past that have reduced runtime performance.
Exceptions can be requested to the security system to mitigate slow performance.

> Updated theory: binaries in `/usr/local/bin` run slow.

# 13. Poking at /usr/local/bin/kubectl

Taking a look at `/usr/local/bin/kubectl`:
```
$ ls -al /usr/local/bin/kubectl
lrwxr-xr-x  1 adam.neumann  admin  55 May 23 11:45 /usr/local/bin/kubectl -> /Applications/Docker.app/Contents/Resources/bin/kubectl
```

Interestingly, `kubectl` is not a file that exists in `/usr/local/bin/` at all, but a symlink to `/Applications/Docker.app/Contents/Resources/bin/kubectl`.
Why is that?!

> Updated theory: binaries in `/Applications/Docker.app/Contents/Resources/bin/` run slow

# 14. Poking at /Applications/Docker.app/Contents/Resources/bin/kubectl

Why is Docker providing my `kubectl`?

It turns out that Docker Desktop installs `kubectl` [when you enable the Docker-based Kubernetes](https://docs.docker.com/desktop/kubernetes/#enable-kubernetes).
I have definitely enabled this is the past (when exploring local kubernetes cluster options).
But I definitely do not have it enabled at the moment.
This option proved too problematic to even set up, let alone use.

The docs do indicate `/usr/local/bin/kubectl` [is removed when you disable Kubernetes](https://docs.docker.com/desktop/kubernetes/#disable-kubernetes), but I'm guessing because my disabling of Kubernetes didn't complete successfully, the `kubectl` symlink was left behind.

It also makes sense why binaries in `/Applications/Docker.app/Contents/Resources/bin/` would be slow - it is such a specific path, and Docker Desktop Kubernetes is such an unused feature that no exception has ever been requested for this path.

We default to Homebrew based packages by default, and I believe the Homebrew directory (`Cellar`) is except already.
I bet if Homebrew managed `kubectl` then it would run fast.

> Updated theory: `kubectl` installed by Homebrew runs fast

# 15. Switching to Homebrew managed kubectl

The first step is to remove the Docker created symlink:
```
$ rm /usr/local/bin/kubectl
```

Second step is to install `kubectl`.
This is done by installing the `kubernetes-cli` package:
```
$ brew install kubernetes-cli
Warning: kubernetes-cli 1.21.0 is already installed, it's just not linked.
To link this version, run:
  brew link kubernetes-cli
```

It turns out this package was already installed, but just not linked.
In Homebrew terminology, linking means creating symlinks from the binaries in the  package-specific directory (i.e. `/usr/local/Cellar/kubernetes-cli/1.21.0/bin/`) to the system directories (i.e. `/usr/local/bin/`).
Linking the package is straight forward:
```
$ brew link kubernetes-cli
Linking /usr/local/Cellar/kubernetes-cli/1.21.0... 225 symlinks created.
```

Now for the moment of truth - does this make `kubectl get pods` fast?
```
$ seq 10 | xargs -I -- time kubectl get pods >> /dev/null
        2.72 real         0.22 user         0.11 sys
        2.49 real         0.21 user         0.09 sys
        2.62 real         0.22 user         0.09 sys
        2.45 real         0.21 user         0.09 sys
        2.62 real         0.21 user         0.09 sys
        2.23 real         0.21 user         0.09 sys
        3.66 real         0.22 user         0.09 sys
        2.53 real         0.20 user         0.08 sys
        2.23 real         0.20 user         0.09 sys
        2.49 real         0.21 user         0.10 sys
```

Yes!!

# TODOs
- why is 1.17.12 GET faster than even curl?