plugins {
    alias(libs.plugins.kotlin.multiplatform)
    alias(libs.plugins.kotlin.serialization)
    `maven-publish`
}

kotlin {
    jvm()
    jvmToolchain(21)

    sourceSets {
        commonMain.dependencies {
            api(project(":foundation"))
            api(libs.coroutines.core)
            implementation(libs.okio)
            implementation(libs.kotlinx.serialization.json)
        }
    }
}

publishing {
    repositories {
        maven {
            name = "localBuildRepo"
            url = uri(rootProject.layout.buildDirectory.dir("local-maven-repo"))
        }
    }
}
