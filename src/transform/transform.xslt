<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" indent="yes"/>
<xsl:param name="removeElementsNamed" select="'w'"/>
    <xsl:template match="@*|node()" name="identity">
    <xsl:copy>
    <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
    </xsl:template>

    <xsl:template match="*">
        <xsl:choose>
            <xsl:when test="not(name() = $removeElementsNamed)">
                <xsl:call-template name="identity"/>
            </xsl:when>
            <xsl:when test="name() = $removeElementsNamed">
                <xsl:copy-of select="text()"/>
            </xsl:when>
        </xsl:choose>
    </xsl:template>
</xsl:stylesheet>