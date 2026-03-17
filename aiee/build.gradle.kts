import org.jetbrains.compose.desktop.application.dsl.TargetFormat

val appId = "me.earzuchan.ryo.aiee"
val ver = "1.0.0"

plugins {
    alias(libs.plugins.kotlin.jvm)
    alias(libs.plugins.kotlin.compose)
    alias(libs.plugins.compose)
    alias(libs.plugins.compose.hotreload)
    alias(libs.plugins.ksp)
    alias(libs.plugins.room)
}

kotlin {
    jvmToolchain(21)
}

dependencies {
    api(project(":modern"))

    implementation(compose.desktop.currentOs)
    implementation(compose.material3)
    implementation(compose.components.resources)
    implementation(compose.preview)

    implementation(libs.koin.core)
    implementation(libs.koin.compose)

    implementation(libs.ktor.client.core)
    implementation(libs.ktor.client.okhttp)

    implementation(libs.datastore.preferences.core)

    implementation(libs.room.runtime)
    implementation(libs.sqlite.bundled)
    ksp(libs.room.compiler)

    implementation(libs.coil.compose)
    implementation(libs.coil.network.ktor3)

    implementation(libs.decompose)
    implementation(libs.decompose.compose)

    implementation(libs.darkmodedetector)
}

compose.desktop {
    application {
        mainClass = "me.earzuchan.ryo.aiee.InitAppKt"

        nativeDistributions {
            targetFormats(TargetFormat.Exe, TargetFormat.Deb, TargetFormat.Dmg)
            packageName = appId
            packageVersion = ver
        }
    }
}

compose.resources {
    publicResClass = false
    packageOfResClass = "$appId.resources"
    generateResClass = auto
}

room {
    schemaDirectory("$projectDir/roomSchemas")
}

compose.resources {
    publicResClass = false
    packageOfResClass = "$appId.resources"
    generateResClass = auto
}