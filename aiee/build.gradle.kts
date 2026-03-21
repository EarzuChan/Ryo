import org.jetbrains.compose.desktop.application.dsl.TargetFormat

val appId = "$group.$name"
val appName = "Ryo"
val appVer = version.toString()

plugins {
    alias(libs.plugins.kotlin.jvm)
    alias(libs.plugins.kotlin.compose)
    alias(libs.plugins.compose)
    alias(libs.plugins.compose.hotreload)
    alias(libs.plugins.ksp)
    alias(libs.plugins.room)
    alias(libs.plugins.buildcfg)
}

kotlin {
    jvmToolchain(21)
}

dependencies {
    api(project(":modern"))

    implementation(compose.material3)
    implementation(compose.components.resources)
    implementation(compose.preview)

    implementation(compose.desktop.currentOs)
    implementation(libs.coroutines.swing)

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
    implementation(libs.reorderable)

    implementation(libs.darkmodedetector)
}

buildConfig {
    packageName(appId)

    buildConfigField("APP_NAME", appName)
    buildConfigField("APP_VERSION", appVer)
    buildConfigField("REPO_URL", "https://github.com/EarzuChan/Ryo")
    buildConfigField("AUTHOR_URL", "https://github.com/EarzuChan")
}

compose.desktop {
    application {
        mainClass = "$appId.InitAppKt"

        nativeDistributions {
            targetFormats(TargetFormat.Exe, TargetFormat.Deb, TargetFormat.Dmg)
            packageName = appId
            packageVersion = appVer
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
