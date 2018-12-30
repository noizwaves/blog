---
title:  "Launch Spring Boot Application during Fluentlenium test"
date:   2017-08-24 13:45:21 -0600
---

Recently I had to set up feature tests for a Spring Boot 1.5.6 + Elm web application.
For this project I decided to use the Fluentlenium 3.3 framework. 
It turns out Spring Boot and Fluentlenium work really well together!

It always seems harder than it should be to launch web apps for feature tests.
Launching background processes, tracking PIDs and killing processes.
All I want to do is start the server, run a test, and stop the server.
After a little searching, this is how you do just that in Spring Boot and Fluentlenium.

```java
@RunWith(SpringRunner.class)
@SpringBootTest(classes = MyApplication.class, webEnvironment = RANDOM_PORT)
public class SomeWorkflowTest extends FluentTest {
    @LocalServerPort
    private int port;
    
    @Test()
    public void testThatFeature() throws Exception {
        goTo("http://" + hostname + ":" + port);
    }
}
```

This code snippet will launch `MyApplication` on a random port, load it up in the configured browser, finish the test, then stop the application.
Ideally, this could get pulled into a BaseFeatureTest abstract class and reused by all test classes in the suite.
