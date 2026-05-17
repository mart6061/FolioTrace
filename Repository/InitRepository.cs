using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;

namespace Repository;

public sealed class InitRepository(IEventRepository eventRepository) : IInitRepository
{
    private static readonly (string Alpha2, string Alpha3, short Numeric, string Name)[] InitialCountryCodes =
    [
        ("AD", "AND", 20, "Andorra"),
        ("AE", "ARE", 784, "United Arab Emirates (the)"),
        ("AF", "AFG", 4, "Afghanistan"),
        ("AG", "ATG", 28, "Antigua and Barbuda"),
        ("AI", "AIA", 660, "Anguilla"),
        ("AL", "ALB", 8, "Albania"),
        ("AM", "ARM", 51, "Armenia"),
        ("AO", "AGO", 24, "Angola"),
        ("AQ", "ATA", 10, "Antarctica"),
        ("AR", "ARG", 32, "Argentina"),
        ("AS", "ASM", 16, "American Samoa"),
        ("AT", "AUT", 40, "Austria"),
        ("AU", "AUS", 36, "Australia"),
        ("AW", "ABW", 533, "Aruba"),
        ("AX", "ALA", 248, "Åland Islands"),
        ("AZ", "AZE", 31, "Azerbaijan"),
        ("BA", "BIH", 70, "Bosnia and Herzegovina"),
        ("BB", "BRB", 52, "Barbados"),
        ("BD", "BGD", 50, "Bangladesh"),
        ("BE", "BEL", 56, "Belgium"),
        ("BF", "BFA", 854, "Burkina Faso"),
        ("BG", "BGR", 100, "Bulgaria"),
        ("BH", "BHR", 48, "Bahrain"),
        ("BI", "BDI", 108, "Burundi"),
        ("BJ", "BEN", 204, "Benin"),
        ("BL", "BLM", 652, "Saint Barthélemy"),
        ("BM", "BMU", 60, "Bermuda"),
        ("BN", "BRN", 96, "Brunei Darussalam"),
        ("BO", "BOL", 68, "Bolivia (Plurinational State of)"),
        ("BQ", "BES", 535, "Bonaire, Sint Eustatius and Saba"),
        ("BR", "BRA", 76, "Brazil"),
        ("BS", "BHS", 44, "Bahamas (the)"),
        ("BT", "BTN", 64, "Bhutan"),
        ("BV", "BVT", 74, "Bouvet Island"),
        ("BW", "BWA", 72, "Botswana"),
        ("BY", "BLR", 112, "Belarus"),
        ("BZ", "BLZ", 84, "Belize"),
        ("CA", "CAN", 124, "Canada"),
        ("CC", "CCK", 166, "Cocos (Keeling) Islands (the)"),
        ("CD", "COD", 180, "Congo (the Democratic Republic of the)"),
        ("CF", "CAF", 140, "Central African Republic (the)"),
        ("CG", "COG", 178, "Congo (the)"),
        ("CH", "CHE", 756, "Switzerland"),
        ("CI", "CIV", 384, "Côte d'Ivoire"),
        ("CK", "COK", 184, "Cook Islands (the)"),
        ("CL", "CHL", 152, "Chile"),
        ("CM", "CMR", 120, "Cameroon"),
        ("CN", "CHN", 156, "China"),
        ("CO", "COL", 170, "Colombia"),
        ("CR", "CRI", 188, "Costa Rica"),
        ("CU", "CUB", 192, "Cuba"),
        ("CV", "CPV", 132, "Cabo Verde"),
        ("CW", "CUW", 531, "Curaçao"),
        ("CX", "CXR", 162, "Christmas Island"),
        ("CY", "CYP", 196, "Cyprus"),
        ("CZ", "CZE", 203, "Czechia"),
        ("DE", "DEU", 276, "Germany"),
        ("DJ", "DJI", 262, "Djibouti"),
        ("DK", "DNK", 208, "Denmark"),
        ("DM", "DMA", 212, "Dominica"),
        ("DO", "DOM", 214, "Dominican Republic (the)"),
        ("DZ", "DZA", 12, "Algeria"),
        ("EC", "ECU", 218, "Ecuador"),
        ("EE", "EST", 233, "Estonia"),
        ("EG", "EGY", 818, "Egypt"),
        ("EH", "ESH", 732, "Western Sahara"),
        ("ER", "ERI", 232, "Eritrea"),
        ("ES", "ESP", 724, "Spain"),
        ("ET", "ETH", 231, "Ethiopia"),
        ("EU", "EUR", 0, "European Union"),
        ("FI", "FIN", 246, "Finland"),
        ("FJ", "FJI", 242, "Fiji"),
        ("FK", "FLK", 238, "Falkland Islands (the) [Malvinas]"),
        ("FM", "FSM", 583, "Micronesia (Federated States of)"),
        ("FO", "FRO", 234, "Faroe Islands (the)"),
        ("FR", "FRA", 250, "France"),
        ("GA", "GAB", 266, "Gabon"),
        ("GB", "GBR", 826, "United Kingdom of Great Britain and Northern Ireland (the)"),
        ("GD", "GRD", 308, "Grenada"),
        ("GE", "GEO", 268, "Georgia"),
        ("GF", "GUF", 254, "French Guiana"),
        ("GG", "GGY", 831, "Guernsey"),
        ("GH", "GHA", 288, "Ghana"),
        ("GI", "GIB", 292, "Gibraltar"),
        ("GL", "GRL", 304, "Greenland"),
        ("GM", "GMB", 270, "Gambia (the)"),
        ("GN", "GIN", 324, "Guinea"),
        ("GP", "GLP", 312, "Guadeloupe"),
        ("GQ", "GNQ", 226, "Equatorial Guinea"),
        ("GR", "GRC", 300, "Greece"),
        ("GS", "SGS", 239, "South Georgia and the South Sandwich Islands"),
        ("GT", "GTM", 320, "Guatemala"),
        ("GU", "GUM", 316, "Guam"),
        ("GW", "GNB", 624, "Guinea-Bissau"),
        ("GY", "GUY", 328, "Guyana"),
        ("HK", "HKG", 344, "Hong Kong"),
        ("HM", "HMD", 334, "Heard Island and McDonald Islands"),
        ("HN", "HND", 340, "Honduras"),
        ("HR", "HRV", 191, "Croatia"),
        ("HT", "HTI", 332, "Haiti"),
        ("HU", "HUN", 348, "Hungary"),
        ("ID", "IDN", 360, "Indonesia"),
        ("IE", "IRL", 372, "Ireland"),
        ("IL", "ISR", 376, "Israel"),
        ("IM", "IMN", 833, "Isle of Man"),
        ("IN", "IND", 356, "India"),
        ("IO", "IOT", 86, "British Indian Ocean Territory (the)"),
        ("IQ", "IRQ", 368, "Iraq"),
        ("IR", "IRN", 364, "Iran (Islamic Republic of)"),
        ("IS", "ISL", 352, "Iceland"),
        ("IT", "ITA", 380, "Italy"),
        ("JE", "JEY", 832, "Jersey"),
        ("JM", "JAM", 388, "Jamaica"),
        ("JO", "JOR", 400, "Jordan"),
        ("JP", "JPN", 392, "Japan"),
        ("KE", "KEN", 404, "Kenya"),
        ("KG", "KGZ", 417, "Kyrgyzstan"),
        ("KH", "KHM", 116, "Cambodia"),
        ("KI", "KIR", 296, "Kiribati"),
        ("KM", "COM", 174, "Comoros (the)"),
        ("KN", "KNA", 659, "Saint Kitts and Nevis"),
        ("KP", "PRK", 408, "Korea (the Democratic People's Republic of)"),
        ("KR", "KOR", 410, "Korea (the Republic of)"),
        ("KW", "KWT", 414, "Kuwait"),
        ("KY", "CYM", 136, "Cayman Islands (the)"),
        ("KZ", "KAZ", 398, "Kazakhstan"),
        ("LA", "LAO", 418, "Lao People's Democratic Republic (the)"),
        ("LB", "LBN", 422, "Lebanon"),
        ("LC", "LCA", 662, "Saint Lucia"),
        ("LI", "LIE", 438, "Liechtenstein"),
        ("LK", "LKA", 144, "Sri Lanka"),
        ("LR", "LBR", 430, "Liberia"),
        ("LS", "LSO", 426, "Lesotho"),
        ("LT", "LTU", 440, "Lithuania"),
        ("LU", "LUX", 442, "Luxembourg"),
        ("LV", "LVA", 428, "Latvia"),
        ("LY", "LBY", 434, "Libya"),
        ("MA", "MAR", 504, "Morocco"),
        ("MC", "MCO", 492, "Monaco"),
        ("MD", "MDA", 498, "Moldova (the Republic of)"),
        ("ME", "MNE", 499, "Montenegro"),
        ("MF", "MAF", 663, "Saint Martin (French part)"),
        ("MG", "MDG", 450, "Madagascar"),
        ("MH", "MHL", 584, "Marshall Islands (the)"),
        ("MK", "MKD", 807, "North Macedonia"),
        ("ML", "MLI", 466, "Mali"),
        ("MM", "MMR", 104, "Myanmar"),
        ("MN", "MNG", 496, "Mongolia"),
        ("MO", "MAC", 446, "Macao"),
        ("MP", "MNP", 580, "Northern Mariana Islands (the)"),
        ("MQ", "MTQ", 474, "Martinique"),
        ("MR", "MRT", 478, "Mauritania"),
        ("MS", "MSR", 500, "Montserrat"),
        ("MT", "MLT", 470, "Malta"),
        ("MU", "MUS", 480, "Mauritius"),
        ("MV", "MDV", 462, "Maldives"),
        ("MW", "MWI", 454, "Malawi"),
        ("MX", "MEX", 484, "Mexico"),
        ("MY", "MYS", 458, "Malaysia"),
        ("MZ", "MOZ", 508, "Mozambique"),
        ("NA", "NAM", 516, "Namibia"),
        ("NC", "NCL", 540, "New Caledonia"),
        ("NE", "NER", 562, "Niger (the)"),
        ("NF", "NFK", 574, "Norfolk Island"),
        ("NG", "NGA", 566, "Nigeria"),
        ("NI", "NIC", 558, "Nicaragua"),
        ("NL", "NLD", 528, "Netherlands (the)"),
        ("NO", "NOR", 578, "Norway"),
        ("NP", "NPL", 524, "Nepal"),
        ("NR", "NRU", 520, "Nauru"),
        ("NU", "NIU", 570, "Niue"),
        ("NZ", "NZL", 554, "New Zealand"),
        ("OM", "OMN", 512, "Oman"),
        ("PA", "PAN", 591, "Panama"),
        ("PE", "PER", 604, "Peru"),
        ("PF", "PYF", 258, "French Polynesia"),
        ("PG", "PNG", 598, "Papua New Guinea"),
        ("PH", "PHL", 608, "Philippines (the)"),
        ("PK", "PAK", 586, "Pakistan"),
        ("PL", "POL", 616, "Poland"),
        ("PM", "SPM", 666, "Saint Pierre and Miquelon"),
        ("PN", "PCN", 612, "Pitcairn"),
        ("PR", "PRI", 630, "Puerto Rico"),
        ("PS", "PSE", 275, "Palestine, State of"),
        ("PT", "PRT", 620, "Portugal"),
        ("PW", "PLW", 585, "Palau"),
        ("PY", "PRY", 600, "Paraguay"),
        ("QA", "QAT", 634, "Qatar"),
        ("RE", "REU", 638, "Réunion"),
        ("RO", "ROU", 642, "Romania"),
        ("RS", "SRB", 688, "Serbia"),
        ("RU", "RUS", 643, "Russian Federation (the)"),
        ("RW", "RWA", 646, "Rwanda"),
        ("SA", "SAU", 682, "Saudi Arabia"),
        ("SB", "SLB", 90, "Solomon Islands"),
        ("SC", "SYC", 690, "Seychelles"),
        ("SD", "SDN", 729, "Sudan (the)"),
        ("SE", "SWE", 752, "Sweden"),
        ("SG", "SGP", 702, "Singapore"),
        ("SH", "SHN", 654, "Saint Helena, Ascension and Tristan da Cunha"),
        ("SI", "SVN", 705, "Slovenia"),
        ("SJ", "SJM", 744, "Svalbard and Jan Mayen"),
        ("SK", "SVK", 703, "Slovakia"),
        ("SL", "SLE", 694, "Sierra Leone"),
        ("SM", "SMR", 674, "San Marino"),
        ("SN", "SEN", 686, "Senegal"),
        ("SO", "SOM", 706, "Somalia"),
        ("SR", "SUR", 740, "Suriname"),
        ("SS", "SSD", 728, "South Sudan"),
        ("ST", "STP", 678, "Sao Tome and Principe"),
        ("SV", "SLV", 222, "El Salvador"),
        ("SX", "SXM", 534, "Sint Maarten (Dutch part)"),
        ("SY", "SYR", 760, "Syrian Arab Republic (the)"),
        ("SZ", "SWZ", 748, "Eswatini"),
        ("TC", "TCA", 796, "Turks and Caicos Islands (the)"),
        ("TD", "TCD", 148, "Chad"),
        ("TF", "ATF", 260, "French Southern Territories (the)"),
        ("TG", "TGO", 768, "Togo"),
        ("TH", "THA", 764, "Thailand"),
        ("TJ", "TJK", 762, "Tajikistan"),
        ("TK", "TKL", 772, "Tokelau"),
        ("TL", "TLS", 626, "Timor-Leste"),
        ("TM", "TKM", 795, "Turkmenistan"),
        ("TN", "TUN", 788, "Tunisia"),
        ("TO", "TON", 776, "Tonga"),
        ("TR", "TUR", 792, "Turkey"),
        ("TT", "TTO", 780, "Trinidad and Tobago"),
        ("TV", "TUV", 798, "Tuvalu"),
        ("TW", "TWN", 158, "Taiwan (Province of China)"),
        ("TZ", "TZA", 834, "Tanzania, the United Republic of"),
        ("UA", "UKR", 804, "Ukraine"),
        ("UG", "UGA", 800, "Uganda"),
        ("UM", "UMI", 581, "United States Minor Outlying Islands (the)"),
        ("US", "USA", 840, "United States of America (the)"),
        ("UY", "URY", 858, "Uruguay"),
        ("UZ", "UZB", 860, "Uzbekistan"),
        ("VA", "VAT", 336, "Holy See (the)"),
        ("VC", "VCT", 670, "Saint Vincent and the Grenadines"),
        ("VE", "VEN", 862, "Venezuela (Bolivarian Republic of)"),
        ("VG", "VGB", 92, "Virgin Islands (British)"),
        ("VI", "VIR", 850, "Virgin Islands (U.S.)"),
        ("VN", "VNM", 704, "Viet Nam"),
        ("VU", "VUT", 548, "Vanuatu"),
        ("WF", "WLF", 876, "Wallis and Futuna"),
        ("WS", "WSM", 882, "Samoa"),
        ("YE", "YEM", 887, "Yemen"),
        ("YT", "MYT", 175, "Mayotte"),
        ("ZA", "ZAF", 710, "South Africa"),
        ("ZM", "ZMB", 894, "Zambia"),
        ("ZW", "ZWE", 716, "Zimbabwe"),
    ];

    private static readonly (string Alpha2, string Svg)[] InitialCountryFlags =
    [
        ("GB", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"flag-icons-gb\" viewBox=\"0 0 640 480\"> <path fill=\"#012169\" d=\"M0 0h640v480H0z\"/> <path fill=\"#FFF\" d=\"m75 0 244 181L562 0h78v62L400 241l240 178v61h-80L320 301 81 480H0v-60l239-178L0 64V0z\"/> <path fill=\"#C8102E\" d=\"m424 281 216 159v40L369 281zm-184 20 6 35L54 480H0zM640 0v3L391 191l2-44L590 0zM0 0l239 176h-60L0 42z\"/> <path fill=\"#FFF\" d=\"M241 0v480h160V0zM0 160v160h640V160z\"/> <path fill=\"#C8102E\" d=\"M0 193v96h640v-96zM273 0v480h96V0z\"/> </svg>"),
        ("EU", "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" id=\"flag-icons-eu\" viewBox=\"0 0 640 480\"> <defs> <g id=\"eu-d\"> <g id=\"eu-b\"> <path id=\"eu-a\" d=\"m0-1-.3 1 .5.1z\"/> <use xlink:href=\"#eu-a\" transform=\"scale(-1 1)\"/> </g> <g id=\"eu-c\"> <use xlink:href=\"#eu-b\" transform=\"rotate(72)\"/> <use xlink:href=\"#eu-b\" transform=\"rotate(144)\"/> </g> <use xlink:href=\"#eu-c\" transform=\"scale(-1 1)\"/> </g> </defs> <path fill=\"#039\" d=\"M0 0h640v480H0z\"/> <g fill=\"#fc0\" transform=\"translate(320 242.3)scale(23.7037)\"> <use xlink:href=\"#eu-d\" width=\"100%\" height=\"100%\" y=\"-6\"/> <use xlink:href=\"#eu-d\" width=\"100%\" height=\"100%\" y=\"6\"/> <g id=\"eu-e\"> <use xlink:href=\"#eu-d\" width=\"100%\" height=\"100%\" x=\"-6\"/> <use xlink:href=\"#eu-d\" width=\"100%\" height=\"100%\" transform=\"rotate(-144 -2.3 -2.1)\"/> <use xlink:href=\"#eu-d\" width=\"100%\" height=\"100%\" transform=\"rotate(144 -2.1 -2.3)\"/> <use xlink:href=\"#eu-d\" width=\"100%\" height=\"100%\" transform=\"rotate(72 -4.7 -2)\"/> <use xlink:href=\"#eu-d\" width=\"100%\" height=\"100%\" transform=\"rotate(72 -5 .5)\"/> </g> <use xlink:href=\"#eu-e\" width=\"100%\" height=\"100%\" transform=\"scale(-1 1)\"/> </g> </svg>"),
        ("FR", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"flag-icons-fr\" viewBox=\"0 0 640 480\"> <path fill=\"#fff\" d=\"M0 0h640v480H0z\"/> <path fill=\"#000091\" d=\"M0 0h213.3v480H0z\"/> <path fill=\"#e1000f\" d=\"M426.7 0H640v480H426.7z\"/> </svg>"),
        ("DE", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"flag-icons-de\" viewBox=\"0 0 640 480\"> <path fill=\"#fc0\" d=\"M0 320h640v160H0z\"/> <path fill=\"#000001\" d=\"M0 0h640v160H0z\"/> <path fill=\"red\" d=\"M0 160h640v160H0z\"/> </svg>"),
        ("US", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"flag-icons-us\" viewBox=\"0 0 640 480\"> <path fill=\"#bd3d44\" d=\"M0 0h640v480H0\"/> <path stroke=\"#fff\" stroke-width=\"37\" d=\"M0 55.3h640M0 129h640M0 203h640M0 277h640M0 351h640M0 425h640\"/> <path fill=\"#192f5d\" d=\"M0 0h364.8v258.5H0\"/> <marker id=\"us-a\" markerHeight=\"30\" markerWidth=\"30\"> <path fill=\"#fff\" d=\"m14 0 9 27L0 10h28L5 27z\"/> </marker> <path fill=\"none\" marker-mid=\"url(#us-a)\" d=\"m0 0 16 11h61 61 61 61 60L47 37h61 61 60 61L16 63h61 61 61 61 60L47 89h61 61 60 61L16 115h61 61 61 61 60L47 141h61 61 60 61L16 166h61 61 61 61 60L47 192h61 61 60 61L16 218h61 61 61 61 60z\"/> </svg>"),
        ("AU", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"flag-icons-au\" viewBox=\"0 0 640 480\"> <path fill=\"#00008B\" d=\"M0 0h640v480H0z\"/> <path fill=\"#fff\" d=\"m37.5 0 122 90.5L281 0h39v31l-120 89.5 120 89V240h-40l-120-89.5L40.5 240H0v-30l119.5-89L0 32V0z\"/> <path fill=\"red\" d=\"M212 140.5 320 220v20l-135.5-99.5zm-92 10 3 17.5-96 72H0zM320 0v1.5l-124.5 94 1-22L295 0zM0 0l119.5 88h-30L0 21z\"/> <path fill=\"#fff\" d=\"M120.5 0v240h80V0zM0 80v80h320V80z\"/> <path fill=\"red\" d=\"M0 96.5v48h320v-48zM136.5 0v240h48V0z\"/> <path fill=\"#fff\" d=\"m527 396.7-20.5 2.6 2.2 20.5-14.8-14.4-14.7 14.5 2-20.5-20.5-2.4 17.3-11.2-10.9-17.5 19.6 6.5 6.9-19.5 7.1 19.4 19.5-6.7-10.7 17.6zm-3.7-117.2 2.7-13-9.8-9 13.2-1.5 5.5-12.1 5.5 12.1 13.2 1.5-9.8 9 2.7 13-11.6-6.6zm-104.1-60-20.3 2.2 1.8 20.3-14.4-14.5-14.8 14.1 2.4-20.3-20.2-2.7 17.3-10.8-10.5-17.5 19.3 6.8L387 178l6.7 19.3 19.4-6.3-10.9 17.3 17.1 11.2ZM623 186.7l-20.9 2.7 2.3 20.9-15.1-14.7-15 14.8 2.1-21-20.9-2.4 17.7-11.5-11.1-17.9 20 6.7 7-19.8 7.2 19.8 19.9-6.9-11 18zm-96.1-83.5-20.7 2.3 1.9 20.8-14.7-14.8-15.1 14.4 2.4-20.7-20.7-2.8 17.7-11L467 73.5l19.7 6.9 7.3-19.5 6.8 19.7 19.8-6.5-11.1 17.6zM234 385.7l-45.8 5.4 4.6 45.9-32.8-32.4-33 32.2 4.9-45.9-45.8-5.8 38.9-24.8-24-39.4 43.6 15 15.8-43.4 15.5 43.5 43.7-14.7-24.3 39.2 38.8 25.1Z\"/> </svg>"),
        ("CH", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"flag-icons-ch\" viewBox=\"0 0 640 480\"> <g fill-rule=\"evenodd\" stroke-width=\"1pt\"> <path fill=\"red\" d=\"M0 0h640v480H0z\"/> <g fill=\"#fff\"> <path d=\"M170 195h300v90H170z\"/> <path d=\"M275 90h90v300h-90z\"/> </g> </g> </svg>"),
        ("CA", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"flag-icons-ca\" viewBox=\"0 0 640 480\"> <path fill=\"#fff\" d=\"M150.1 0h339.7v480H150z\"/> <path fill=\"#d52b1e\" d=\"M-19.7 0h169.8v480H-19.7zm509.5 0h169.8v480H489.9zM201 232l-13.3 4.4 61.4 54c4.7 13.7-1.6 17.8-5.6 25l66.6-8.4-1.6 67 13.9-.3-3.1-66.6 66.7 8c-4.1-8.7-7.8-13.3-4-27.2l61.3-51-10.7-4c-8.8-6.8 3.8-32.6 5.6-48.9 0 0-35.7 12.3-38 5.8l-9.2-17.5-32.6 35.8c-3.5.9-5-.5-5.9-3.5l15-74.8-23.8 13.4q-3.2 1.3-5.2-2.2l-23-46-23.6 47.8q-2.8 2.5-5 .7L264 130.8l13.7 74.1c-1.1 3-3.7 3.8-6.7 2.2l-31.2-35.3c-4 6.5-6.8 17.1-12.2 19.5s-23.5-4.5-35.6-7c4.2 14.8 17 39.6 9 47.7\"/> </svg>"),
        ("JP", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"flag-icons-jp\" viewBox=\"0 0 640 480\"> <defs> <clipPath id=\"jp-a\"> <path fill-opacity=\".7\" d=\"M-88 32h640v480H-88z\"/> </clipPath> </defs> <g fill-rule=\"evenodd\" stroke-width=\"1pt\" clip-path=\"url(#jp-a)\" transform=\"translate(88 -32)\"> <path fill=\"#fff\" d=\"M-128 32h720v480h-720z\"/> <circle cx=\"523.1\" cy=\"344.1\" r=\"194.9\" fill=\"#bc002d\" transform=\"translate(-168.4 8.6)scale(.76554)\"/> </g> </svg>"),
        ("CN", "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" id=\"flag-icons-cn\" viewBox=\"0 0 640 480\"> <defs> <path id=\"cn-a\" fill=\"#ff0\" d=\"M-.6.8 0-1 .6.8-1-.3h2z\"/> </defs> <path fill=\"#ee1c25\" d=\"M0 0h640v480H0z\"/> <use xlink:href=\"#cn-a\" width=\"30\" height=\"20\" transform=\"matrix(71.9991 0 0 72 120 120)\"/> <use xlink:href=\"#cn-a\" width=\"30\" height=\"20\" transform=\"matrix(-12.33562 -20.5871 20.58684 -12.33577 240.3 48)\"/> <use xlink:href=\"#cn-a\" width=\"30\" height=\"20\" transform=\"matrix(-3.38573 -23.75998 23.75968 -3.38578 288 95.8)\"/> <use xlink:href=\"#cn-a\" width=\"30\" height=\"20\" transform=\"matrix(6.5991 -23.0749 23.0746 6.59919 288 168)\"/> <use xlink:href=\"#cn-a\" width=\"30\" height=\"20\" transform=\"matrix(14.9991 -18.73557 18.73533 14.99929 240 216)\"/> </svg>"),
        ("IN", "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" id=\"flag-icons-in\" viewBox=\"0 0 640 480\"> <path fill=\"#f93\" d=\"M0 0h640v160H0z\"/> <path fill=\"#fff\" d=\"M0 160h640v160H0z\"/> <path fill=\"#128807\" d=\"M0 320h640v160H0z\"/> <g transform=\"matrix(3.2 0 0 3.2 320 240)\"> <circle r=\"20\" fill=\"#008\"/> <circle r=\"17.5\" fill=\"#fff\"/> <circle r=\"3.5\" fill=\"#008\"/> <g id=\"in-d\"> <g id=\"in-c\"> <g id=\"in-b\"> <g id=\"in-a\" fill=\"#008\"> <circle r=\".9\" transform=\"rotate(7.5 -8.8 133.5)\"/> <path d=\"M0 17.5.6 7 0 2l-.6 5z\"/> </g> <use xlink:href=\"#in-a\" width=\"100%\" height=\"100%\" transform=\"rotate(15)\"/> </g> <use xlink:href=\"#in-b\" width=\"100%\" height=\"100%\" transform=\"rotate(30)\"/> </g> <use xlink:href=\"#in-c\" width=\"100%\" height=\"100%\" transform=\"rotate(60)\"/> </g> <use xlink:href=\"#in-d\" width=\"100%\" height=\"100%\" transform=\"rotate(120)\"/> <use xlink:href=\"#in-d\" width=\"100%\" height=\"100%\" transform=\"rotate(-120)\"/> </g> </svg>"),
        ("NZ", "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" id=\"flag-icons-nz\" viewBox=\"0 0 640 480\"> <defs> <g id=\"nz-b\"> <g id=\"nz-a\"> <path d=\"M0-.3v.5l1-.5z\"/> <path d=\"M.2.3 0-.1l1-.2z\"/> </g> <use xlink:href=\"#nz-a\" transform=\"scale(-1 1)\"/> <use xlink:href=\"#nz-a\" transform=\"rotate(72 0 0)\"/> <use xlink:href=\"#nz-a\" transform=\"rotate(-72 0 0)\"/> <use xlink:href=\"#nz-a\" transform=\"scale(-1 1)rotate(72)\"/> </g> </defs> <path fill=\"#00247d\" fill-rule=\"evenodd\" d=\"M0 0h640v480H0z\"/> <g transform=\"translate(-111 36.1)scale(.66825)\"> <use xlink:href=\"#nz-b\" width=\"100%\" height=\"100%\" fill=\"#fff\" transform=\"translate(900 120)scale(45.4)\"/> <use xlink:href=\"#nz-b\" width=\"100%\" height=\"100%\" fill=\"#cc142b\" transform=\"matrix(30 0 0 30 900 120)\"/> </g> <g transform=\"rotate(82 525.2 114.6)scale(.66825)\"> <use xlink:href=\"#nz-b\" width=\"100%\" height=\"100%\" fill=\"#fff\" transform=\"rotate(-82 519 -457.7)scale(40.4)\"/> <use xlink:href=\"#nz-b\" width=\"100%\" height=\"100%\" fill=\"#cc142b\" transform=\"rotate(-82 519 -457.7)scale(25)\"/> </g> <g transform=\"rotate(82 525.2 114.6)scale(.66825)\"> <use xlink:href=\"#nz-b\" width=\"100%\" height=\"100%\" fill=\"#fff\" transform=\"rotate(-82 668.6 -327.7)scale(45.4)\"/> <use xlink:href=\"#nz-b\" width=\"100%\" height=\"100%\" fill=\"#cc142b\" transform=\"rotate(-82 668.6 -327.7)scale(30)\"/> </g> <g transform=\"translate(-111 36.1)scale(.66825)\"> <use xlink:href=\"#nz-b\" width=\"100%\" height=\"100%\" fill=\"#fff\" transform=\"translate(900 480)scale(50.4)\"/> <use xlink:href=\"#nz-b\" width=\"100%\" height=\"100%\" fill=\"#cc142b\" transform=\"matrix(35 0 0 35 900 480)\"/> </g> <path fill=\"#012169\" d=\"M0 0h320v240H0z\"/> <path fill=\"#fff\" d=\"m37.5 0 122 90.5L281 0h39v31l-120 89.5 120 89V240h-40l-120-89.5L40.5 240H0v-30l119.5-89L0 32V0z\"/> <path fill=\"#c8102e\" d=\"M212 140.5 320 220v20l-135.5-99.5zm-92 10 3 17.5-96 72H0zM320 0v1.5l-124.5 94 1-22L295 0zM0 0l119.5 88h-30L0 21z\"/> <path fill=\"#fff\" d=\"M120.5 0v240h80V0zM0 80v80h320V80z\"/> <path fill=\"#c8102e\" d=\"M0 96.5v48h320v-48zM136.5 0v240h48V0z\"/> </svg>"),
        ("IE", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"flag-icons-ie\" viewBox=\"0 0 640 480\"> <g fill-rule=\"evenodd\" stroke-width=\"1pt\"> <path fill=\"#fff\" d=\"M0 0h640v480H0z\"/> <path fill=\"#009A49\" d=\"M0 0h213.3v480H0z\"/> <path fill=\"#FF7900\" d=\"M426.7 0H640v480H426.7z\"/> </g> </svg>"),
        ("ES", "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 640 480\"><path fill=\"#c60b1e\" d=\"M0 0h640v480H0z\"/><path fill=\"#ffc400\" d=\"M0 120h640v240H0z\"/></svg>"),
        ("IT", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"flag-icons-it\" viewBox=\"0 0 640 480\"> <g fill-rule=\"evenodd\" stroke-width=\"1pt\"> <path fill=\"#fff\" d=\"M0 0h640v480H0z\"/> <path fill=\"#009246\" d=\"M0 0h213.3v480H0z\"/> <path fill=\"#ce2b37\" d=\"M426.7 0H640v480H426.7z\"/> </g> </svg>"),
        ("NL", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"flag-icons-nl\" viewBox=\"0 0 640 480\"> <path fill=\"#ae1c28\" d=\"M0 0h640v160H0z\"/> <path fill=\"#fff\" d=\"M0 160h640v160H0z\"/> <path fill=\"#21468b\" d=\"M0 320h640v160H0z\"/> </svg>"),
        ("SE", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"flag-icons-se\" viewBox=\"0 0 640 480\"> <path fill=\"#005293\" d=\"M0 0h640v480H0z\"/> <path fill=\"#fecb00\" d=\"M176 0v192H0v96h176v192h96V288h368v-96H272V0z\"/> </svg>"),
        ("SG", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"flag-icons-sg\" viewBox=\"0 0 640 480\"> <defs> <clipPath id=\"sg-a\"> <path fill-opacity=\".7\" d=\"M0 0h640v480H0z\"/> </clipPath> </defs> <g fill-rule=\"evenodd\" clip-path=\"url(#sg-a)\"> <path fill=\"#fff\" d=\"M-20 0h720v480H-20z\"/> <path fill=\"#df0000\" d=\"M-20 0h720v240H-20z\"/> <path fill=\"#fff\" d=\"M146 40.2a84.4 84.4 0 0 0 .8 165.2 86 86 0 0 1-106.6-59 86 86 0 0 1 59-106c16-4.6 30.8-4.7 46.9-.2z\"/> <path fill=\"#fff\" d=\"m133 110 4.9 15-13-9.2-12.8 9.4 4.7-15.2-12.8-9.3 15.9-.2 5-15 5 15h15.8zm17.5 52 5 15.1-13-9.2-12.9 9.3 4.8-15.1-12.8-9.4 15.9-.1 4.9-15.1 5 15h16zm58.5-.4 4.9 15.2-13-9.3-12.8 9.3 4.7-15.1-12.8-9.3 15.9-.2 5-15 5 15h15.8zm17.4-51.6 4.9 15.1-13-9.2-12.8 9.3 4.8-15.1-12.9-9.4 16-.1 4.8-15.1 5 15h16zm-46.3-34.3 5 15.2-13-9.3-12.9 9.4 4.8-15.2-12.8-9.4 15.8-.1 5-15.1 5 15h16z\"/> </g> </svg>"),
    ];
    public async Task Build(CancellationToken cancellationToken = default)
    {
        await DeleteEvents(cancellationToken);
        await CreateSetupEvents(cancellationToken);
    }

    private async Task DeleteEvents(CancellationToken cancellationToken)
    {
        await eventRepository.ClearAsync(cancellationToken);
    }

    private async Task CreateSetupEvents(CancellationToken cancellationToken)
    {
        await CreateCountrySetupEvents(cancellationToken);
    }

    private async Task CreateCountrySetupEvents(CancellationToken cancellationToken)
    {
        await StoreEvents<Countries, CountryCreatedEvent>(
            Constants.Initialisation.CountriesStreamId,
            CreateInitialCountryCreatedEvents(),
            cancellationToken);

        await AppendEvents(
            Constants.Initialisation.CountriesStreamId,
            CreateInitialCountryFlagModifiedEvents(),
            cancellationToken);
    }

    public static IReadOnlyList<CountryCreatedEvent> CreateInitialCountryCreatedEvents() =>
        InitialCountryCodes
            .Select(country => CountryCreatedEventBuilder.Create(
                Guid.NewGuid(),
                Constants.Initialisation.EventDateTime,
                Constants.Initialisation.AuditDateTime,
                Constants.Initialisation.Reason,
                country.Alpha2,
                country.Alpha3,
                country.Numeric,
                country.Name))
            .ToList();

    public static IReadOnlyList<CountryFlagModifiedEvent> CreateInitialCountryFlagModifiedEvents()
    {
        var eventDateTime = EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(1));
        var auditDateTime = AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(1));

        return InitialCountryFlags
            .Select(country => CountryFlagModifiedEventBuilder.Create(
                Guid.NewGuid(),
                eventDateTime,
                auditDateTime,
                Constants.Initialisation.Reason,
                country.Alpha2,
                new CountryFlag(country.Svg)))
            .ToList();
    }
    private async Task StoreEvents<TAggregate, TEvent>(Guid streamId, IEnumerable<TEvent> events, CancellationToken cancellationToken)
        where TAggregate : class, IAggregate
        where TEvent : class, IEventBase
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        var eventData = events.ToList();
        if (eventData.Count == 0)
            return;

        await eventRepository.StartStreamAsync<TAggregate, TEvent>(streamId, eventData, cancellationToken);
    }

    private async Task AppendEvents<TEvent>(Guid streamId, IEnumerable<TEvent> events, CancellationToken cancellationToken)
        where TEvent : class, IEventBase
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        foreach (var @event in events)
            await eventRepository.AppendAsync(streamId, @event, cancellationToken);
    }
}









