// swift-tools-version: 6.0
import PackageDescription

let package = Package(
	name: "KapuschFacebookAuthInterop",
	platforms: [
		.iOS(.v15),
	],
	products: [
		.library(
			name: "KapuschFacebookAuthInterop",
			type: .static,
			targets: ["KapuschFacebookAuthInterop"]
		),
	],
	dependencies: [
		.package(url: "https://github.com/facebook/facebook-ios-sdk", from: "18.0.1"),
	],
	targets: [
		.target(
			name: "KapuschFacebookAuthInterop",
			dependencies: [
				.product(name: "FacebookLogin", package: "facebook-ios-sdk"),
				.product(name: "FacebookCore", package: "facebook-ios-sdk"),
			],
			path: "Sources/KapuschFacebookAuthInterop"
		),
	]
)
