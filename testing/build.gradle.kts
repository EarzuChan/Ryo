plugins {
    alias(libs.plugins.kotlin.jvm)
}

kotlin {
    jvmToolchain(21)
}

dependencies {
    implementation(project(":foundation"))
    implementation(project(":modern"))

    testImplementation(kotlin("test"))
}

tasks.test {
    useJUnitPlatform()
}
