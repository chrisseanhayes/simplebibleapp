<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="xml" indent="yes"/>
    <xsl:param name="wordxml" select="document('sf_w_bk_ch_vs.xml')" />

    <xsl:template match="@*|node()" name="identity">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>
    <xsl:template match="*">
        <xsl:choose>
            <xsl:when test="self::text()">
                <xsl:param name="chapter" select="[ancestor::chapter]/@osisID" />
            </xsl:when>
            <xsl:when test="not(self::text())">
                <xsl:call-template name="identity"/>
            </xsl:when>
        </xsl:choose>
    </xsl:template>
</xsl:stylesheet>