<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" indent="yes"/>
<xsl:param name="removeElementsNamed" select="'w'"/>
    <xsl:template match="@*|node()" name="identity">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="/">
        <xsl:element name="osis">
            <xsl:element name="osisText">
                <xsl:for-each select="/XMLBIBLE/BIBLEBOOK">
                    <xsl:variable name="bsname" select="@bsname"/>
                    <xsl:text>&#xa;</xsl:text>
                    <xsl:element name="div">
                        <xsl:attribute name="type">
                            <xsl:text>book</xsl:text>
                        </xsl:attribute>
                        <xsl:attribute name="osisID">
                            <xsl:value-of select="$bsname"/>
                        </xsl:attribute>
                        <xsl:attribute name="canonical">
                            <xsl:text>true</xsl:text>
                        </xsl:attribute>
                        <xsl:text>&#xa;</xsl:text>
                        <xsl:for-each select="./CHAPTER">
                            <xsl:variable name="cnumber" select="@cnumber"/>
                            <xsl:element name="chapter">
                                <xsl:attribute name="osisID">
                                    <xsl:value-of select="$bsname"/>
                                    <xsl:text>.</xsl:text>
                                    <xsl:value-of select="$cnumber"/>
                                </xsl:attribute>
                                <xsl:for-each select="./VERS">
                                    <xsl:variable name="vnumber" select="@vnumber"/>
                                    <xsl:text>&#xa;</xsl:text>
                                    <xsl:element name="verse">
                                        <xsl:attribute name="osisID">
                                            <xsl:value-of select="$bsname"/>
                                            <xsl:text>.</xsl:text>
                                            <xsl:value-of select="$cnumber"/>
                                            <xsl:text>.</xsl:text>
                                            <xsl:value-of select="$vnumber"/>
                                        </xsl:attribute>
                                        <xsl:attribute name="sID">
                                            <xsl:value-of select="$bsname"/>
                                            <xsl:text>.</xsl:text>
                                            <xsl:value-of select="$cnumber"/>
                                            <xsl:text>.</xsl:text>
                                            <xsl:value-of select="$vnumber"/>
                                        </xsl:attribute>
                                    </xsl:element>
                                    <xsl:copy-of select="node()"/>
                                    <xsl:element name="verse">
                                        <xsl:attribute name="eID">
                                            <xsl:value-of select="$bsname"/>
                                            <xsl:text>.</xsl:text>
                                            <xsl:value-of select="$cnumber"/>
                                            <xsl:text>.</xsl:text>
                                            <xsl:value-of select="$vnumber"/>
                                        </xsl:attribute>
                                    </xsl:element>
                                </xsl:for-each>
                                <xsl:text>&#xa;</xsl:text>
                            </xsl:element>
                            <xsl:text>&#xa;</xsl:text>
                        </xsl:for-each>
                    </xsl:element>
                </xsl:for-each>
            </xsl:element>
        </xsl:element>
    </xsl:template>
</xsl:stylesheet>